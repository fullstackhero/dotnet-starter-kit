################################################################################
# S3 Bucket
################################################################################

resource "aws_s3_bucket" "this" {
  bucket        = var.name
  force_destroy = var.force_destroy

  tags = merge(var.tags, {
    Name = var.name
  })
}

################################################################################
# Bucket Ownership Controls
################################################################################

resource "aws_s3_bucket_ownership_controls" "this" {
  bucket = aws_s3_bucket.this.id

  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

################################################################################
# Versioning
################################################################################

resource "aws_s3_bucket_versioning" "this" {
  bucket = aws_s3_bucket.this.id

  versioning_configuration {
    status = var.versioning_enabled ? "Enabled" : "Suspended"
  }
}

################################################################################
# Server-Side Encryption
################################################################################

resource "aws_s3_bucket_server_side_encryption_configuration" "this" {
  bucket = aws_s3_bucket.this.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm     = var.kms_key_arn != null ? "aws:kms" : "AES256"
      kms_master_key_id = var.kms_key_arn
    }
    bucket_key_enabled = var.kms_key_arn != null
  }
}

################################################################################
# Public Access Block
################################################################################

resource "aws_s3_bucket_public_access_block" "this" {
  bucket                  = aws_s3_bucket.this.id
  block_public_acls       = true
  ignore_public_acls      = true
  block_public_policy     = var.enable_public_read ? false : true
  restrict_public_buckets = var.enable_public_read ? false : true
}

################################################################################
# Lifecycle Rules
################################################################################

resource "aws_s3_bucket_lifecycle_configuration" "this" {
  count = length(var.lifecycle_rules) > 0 || var.enable_intelligent_tiering ? 1 : 0

  bucket = aws_s3_bucket.this.id

  dynamic "rule" {
    for_each = var.lifecycle_rules
    content {
      id     = rule.value.id
      status = rule.value.enabled ? "Enabled" : "Disabled"

      filter {
        prefix = rule.value.prefix
      }

      dynamic "transition" {
        for_each = rule.value.transitions
        content {
          days          = transition.value.days
          storage_class = transition.value.storage_class
        }
      }

      dynamic "expiration" {
        for_each = rule.value.expiration_days != null ? [1] : []
        content {
          days = rule.value.expiration_days
        }
      }

      dynamic "noncurrent_version_transition" {
        for_each = rule.value.noncurrent_version_transitions
        content {
          noncurrent_days = noncurrent_version_transition.value.days
          storage_class   = noncurrent_version_transition.value.storage_class
        }
      }

      dynamic "noncurrent_version_expiration" {
        for_each = rule.value.noncurrent_version_expiration_days != null ? [1] : []
        content {
          noncurrent_days = rule.value.noncurrent_version_expiration_days
        }
      }

      dynamic "abort_incomplete_multipart_upload" {
        for_each = rule.value.abort_incomplete_multipart_upload_days != null ? [1] : []
        content {
          days_after_initiation = rule.value.abort_incomplete_multipart_upload_days
        }
      }
    }
  }

  dynamic "rule" {
    for_each = var.enable_intelligent_tiering ? [1] : []
    content {
      id     = "intelligent-tiering"
      status = "Enabled"

      filter {
        prefix = ""
      }

      transition {
        storage_class = "INTELLIGENT_TIERING"
      }
    }
  }

  depends_on = [aws_s3_bucket_versioning.this]
}

################################################################################
# CORS Configuration
################################################################################

resource "aws_s3_bucket_cors_configuration" "this" {
  count = length(var.cors_rules) > 0 ? 1 : 0

  bucket = aws_s3_bucket.this.id

  dynamic "cors_rule" {
    for_each = var.cors_rules
    content {
      allowed_headers = cors_rule.value.allowed_headers
      allowed_methods = cors_rule.value.allowed_methods
      allowed_origins = cors_rule.value.allowed_origins
      expose_headers  = cors_rule.value.expose_headers
      max_age_seconds = cors_rule.value.max_age_seconds
    }
  }
}

################################################################################
# CloudFront Origin Access Control
################################################################################

resource "aws_cloudfront_origin_access_control" "this" {
  count = var.enable_cloudfront ? 1 : 0

  name                              = "${aws_s3_bucket.this.bucket}-oac"
  description                       = "Access control for ${aws_s3_bucket.this.bucket}"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

################################################################################
# CloudFront Distribution
################################################################################

resource "aws_cloudfront_distribution" "this" {
  count = var.enable_cloudfront ? 1 : 0

  enabled             = true
  comment             = var.cloudfront_comment != "" ? var.cloudfront_comment : "Public assets for ${aws_s3_bucket.this.bucket}"
  price_class         = var.cloudfront_price_class
  default_root_object = var.cloudfront_default_root_object
  aliases             = var.cloudfront_aliases
  http_version        = "http2and3"

  origin {
    domain_name              = aws_s3_bucket.this.bucket_regional_domain_name
    origin_id                = "s3-${aws_s3_bucket.this.bucket}"
    origin_access_control_id = aws_cloudfront_origin_access_control.this[0].id
  }

  default_cache_behavior {
    target_origin_id       = "s3-${aws_s3_bucket.this.bucket}"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true

    cache_policy_id          = var.cloudfront_cache_policy_id
    origin_request_policy_id = var.cloudfront_origin_request_policy_id

    dynamic "forwarded_values" {
      for_each = var.cloudfront_cache_policy_id == null ? [1] : []
      content {
        query_string = false
        cookies {
          forward = "none"
        }
      }
    }
  }

  restrictions {
    geo_restriction {
      restriction_type = var.cloudfront_geo_restriction_type
      locations        = var.cloudfront_geo_restriction_locations
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = var.cloudfront_acm_certificate_arn == null
    acm_certificate_arn            = var.cloudfront_acm_certificate_arn
    ssl_support_method             = var.cloudfront_acm_certificate_arn != null ? "sni-only" : null
    minimum_protocol_version       = var.cloudfront_acm_certificate_arn != null ? "TLSv1.2_2021" : null
  }

  tags = var.tags
}

################################################################################
# Bucket Policy
################################################################################

locals {
  bucket_policy_statements = concat(
    # Enforce SSL/TLS for all requests
    [
      {
        Sid       = "EnforceSSLOnly"
        Effect    = "Deny"
        Principal = "*"
        Action    = "s3:*"
        Resource = [
          "arn:aws:s3:::${aws_s3_bucket.this.bucket}",
          "arn:aws:s3:::${aws_s3_bucket.this.bucket}/*"
        ]
        Condition = {
          Bool = {
            "aws:SecureTransport" = "false"
          }
        }
      },
      {
        Sid       = "EnforceTLSVersion"
        Effect    = "Deny"
        Principal = "*"
        Action    = "s3:*"
        Resource = [
          "arn:aws:s3:::${aws_s3_bucket.this.bucket}",
          "arn:aws:s3:::${aws_s3_bucket.this.bucket}/*"
        ]
        Condition = {
          NumericLessThan = {
            "s3:TlsVersion" = "1.2"
          }
        }
      }
    ],
    var.enable_public_read && length(var.public_read_prefix) > 0 ? [
      {
        Sid       = "AllowPublicReadUploads"
        Effect    = "Allow"
        Principal = "*"
        Action    = ["s3:GetObject"]
        Resource  = "arn:aws:s3:::${aws_s3_bucket.this.bucket}/${var.public_read_prefix}*"
      }
    ] : [],
    var.enable_cloudfront ? [
      {
        Sid    = "AllowCloudFrontRead"
        Effect = "Allow"
        Principal = {
          Service = "cloudfront.amazonaws.com"
        }
        Action   = ["s3:GetObject"]
        Resource = "arn:aws:s3:::${aws_s3_bucket.this.bucket}/*"
        Condition = {
          StringEquals = {
            "AWS:SourceArn" = aws_cloudfront_distribution.this[0].arn
          }
        }
      }
    ] : [],
    var.additional_bucket_policy_statements
  )
}

resource "aws_s3_bucket_policy" "this" {
  bucket = aws_s3_bucket.this.id
  policy = jsonencode({
    Version   = "2012-10-17"
    Statement = local.bucket_policy_statements
  })

  depends_on = [aws_s3_bucket_public_access_block.this]
}
