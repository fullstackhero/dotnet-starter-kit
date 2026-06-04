################################################################################
# static_site
#
# Private S3 origin + CloudFront (Origin Access Control) for hosting a
# single-page application. The bucket is never public; CloudFront is the only
# reader. 403/404 responses fall back to the SPA entry point for client-side
# routing, and an optional runtime config.json is published with caching
# disabled so a single build artifact can be promoted across environments.
################################################################################

locals {
  origin_id   = "s3-${var.name}"
  use_acm     = var.acm_certificate_arn != null
  config_json = var.runtime_config != null ? jsonencode(var.runtime_config) : ""

  # Explicit policy ID wins; otherwise attach the managed security-headers
  # policy (HSTS, nosniff, frame, referrer) unless disabled.
  response_headers_policy_id = var.response_headers_policy_id != null ? var.response_headers_policy_id : (
    var.security_headers_enabled ? data.aws_cloudfront_response_headers_policy.security[0].id : null
  )
}

data "aws_cloudfront_response_headers_policy" "security" {
  count = var.response_headers_policy_id == null && var.security_headers_enabled ? 1 : 0
  name  = var.response_headers_policy_name
}

################################################################################
# S3 Bucket (private origin)
################################################################################

resource "aws_s3_bucket" "this" {
  bucket        = var.name
  force_destroy = var.force_destroy

  tags = merge(var.tags, {
    Name = var.name
  })
}

resource "aws_s3_bucket_ownership_controls" "this" {
  bucket = aws_s3_bucket.this.id

  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_public_access_block" "this" {
  bucket = aws_s3_bucket.this.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "this" {
  bucket = aws_s3_bucket.this.id

  versioning_configuration {
    status = var.versioning_enabled ? "Enabled" : "Suspended"
  }
}

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
# CloudFront Origin Access Control + managed cache policies
################################################################################

resource "aws_cloudfront_origin_access_control" "this" {
  name                              = "${var.name}-oac"
  description                       = "OAC for ${var.name}"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

data "aws_cloudfront_cache_policy" "assets" {
  name = var.cache_policy_name
}

# Runtime config must never be cached so promotions take effect immediately.
data "aws_cloudfront_cache_policy" "disabled" {
  name = "Managed-CachingDisabled"
}

################################################################################
# CloudFront Distribution
################################################################################

resource "aws_cloudfront_distribution" "this" {
  enabled             = true
  is_ipv6_enabled     = true
  comment             = var.comment != "" ? var.comment : "Static site for ${var.name}"
  price_class         = var.price_class
  default_root_object = var.default_root_object
  aliases             = var.aliases
  http_version        = "http2and3"

  origin {
    domain_name              = aws_s3_bucket.this.bucket_regional_domain_name
    origin_id                = local.origin_id
    origin_access_control_id = aws_cloudfront_origin_access_control.this.id
  }

  default_cache_behavior {
    target_origin_id           = local.origin_id
    viewer_protocol_policy     = "redirect-to-https"
    allowed_methods            = ["GET", "HEAD", "OPTIONS"]
    cached_methods             = ["GET", "HEAD"]
    compress                   = true
    cache_policy_id            = data.aws_cloudfront_cache_policy.assets.id
    response_headers_policy_id = local.response_headers_policy_id
  }

  # Never cache the runtime config file at the edge.
  dynamic "ordered_cache_behavior" {
    for_each = var.runtime_config != null ? [1] : []
    content {
      path_pattern               = var.runtime_config_object_key
      target_origin_id           = local.origin_id
      viewer_protocol_policy     = "redirect-to-https"
      allowed_methods            = ["GET", "HEAD", "OPTIONS"]
      cached_methods             = ["GET", "HEAD"]
      compress                   = true
      cache_policy_id            = data.aws_cloudfront_cache_policy.disabled.id
      response_headers_policy_id = local.response_headers_policy_id
    }
  }

  # SPA client-side routing: serve the entry point for unknown paths.
  dynamic "custom_error_response" {
    for_each = var.spa_fallback_enabled ? [403, 404] : []
    content {
      error_code            = custom_error_response.value
      response_code         = 200
      response_page_path    = var.spa_fallback_path
      error_caching_min_ttl = 10
    }
  }

  restrictions {
    geo_restriction {
      restriction_type = var.geo_restriction_type
      locations        = var.geo_restriction_locations
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = local.use_acm ? null : true
    acm_certificate_arn            = var.acm_certificate_arn
    ssl_support_method             = local.use_acm ? "sni-only" : null
    minimum_protocol_version       = local.use_acm ? var.minimum_protocol_version : null
  }

  tags = var.tags
}

################################################################################
# Bucket Policy — CloudFront read (OAC) + TLS enforcement
################################################################################

resource "aws_s3_bucket_policy" "this" {
  bucket = aws_s3_bucket.this.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "AllowCloudFrontRead"
        Effect    = "Allow"
        Principal = { Service = "cloudfront.amazonaws.com" }
        Action    = ["s3:GetObject"]
        Resource  = "${aws_s3_bucket.this.arn}/*"
        Condition = {
          StringEquals = {
            "AWS:SourceArn" = aws_cloudfront_distribution.this.arn
          }
        }
      },
      {
        Sid       = "EnforceSSLOnly"
        Effect    = "Deny"
        Principal = "*"
        Action    = "s3:*"
        Resource  = [aws_s3_bucket.this.arn, "${aws_s3_bucket.this.arn}/*"]
        Condition = {
          Bool = { "aws:SecureTransport" = "false" }
        }
      }
    ]
  })

  depends_on = [aws_s3_bucket_public_access_block.this]
}

################################################################################
# Runtime config (config.json) — optional
################################################################################

resource "aws_s3_object" "runtime_config" {
  count = var.runtime_config != null ? 1 : 0

  bucket        = aws_s3_bucket.this.id
  key           = var.runtime_config_object_key
  content       = local.config_json
  content_type  = "application/json"
  cache_control = "no-store"
  etag          = md5(local.config_json)
}
