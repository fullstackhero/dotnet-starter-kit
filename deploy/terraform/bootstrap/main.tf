################################################################################
# Terraform Bootstrap - State Backend Infrastructure
#
# This module creates an S3 bucket for storing Terraform state.
# Starting with Terraform 1.10+, S3 native locking is used via use_lockfile.
# DynamoDB is no longer required for state locking.
################################################################################

terraform {
  required_version = ">= 1.15.4"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.46"
    }
  }

  backend "local" {}
}

provider "aws" {
  region = var.region

  default_tags {
    tags = {
      ManagedBy = "terraform"
      Purpose   = "terraform-state"
    }
  }
}

################################################################################
# S3 Bucket for Terraform State
################################################################################

resource "aws_s3_bucket" "tf_state" {
  bucket = var.bucket_name

  lifecycle {
    prevent_destroy = true
  }

  tags = {
    Name        = var.bucket_name
    Description = "Terraform state storage"
  }
}

resource "aws_s3_bucket_versioning" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm     = var.kms_key_arn != null ? "aws:kms" : "AES256"
      kms_master_key_id = var.kms_key_arn
    }
    bucket_key_enabled = var.kms_key_arn != null
  }
}

resource "aws_s3_bucket_public_access_block" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_lifecycle_configuration" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id

  rule {
    id     = "abort-incomplete-uploads"
    status = "Enabled"

    filter {
      prefix = ""
    }

    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }
  }

  rule {
    id     = "noncurrent-version-expiration"
    status = "Enabled"

    filter {
      prefix = ""
    }

    noncurrent_version_expiration {
      noncurrent_days = var.state_version_retention_days
    }
  }

  rule {
    id     = "lockfile-cleanup"
    status = "Enabled"

    filter {
      prefix = ""
    }

    expiration {
      expired_object_delete_marker = true
    }
  }

  depends_on = [aws_s3_bucket_versioning.tf_state]
}

################################################################################
# S3 Bucket Policy - Enforce SSL and Required Permissions for Native Locking
################################################################################

resource "aws_s3_bucket_policy" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "EnforceSSLOnly"
        Effect    = "Deny"
        Principal = "*"
        Action    = "s3:*"
        Resource = [
          aws_s3_bucket.tf_state.arn,
          "${aws_s3_bucket.tf_state.arn}/*"
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
          aws_s3_bucket.tf_state.arn,
          "${aws_s3_bucket.tf_state.arn}/*"
        ]
        Condition = {
          NumericLessThan = {
            "s3:TlsVersion" = "1.2"
          }
        }
      }
    ]
  })

  depends_on = [aws_s3_bucket_public_access_block.tf_state]
}

################################################################################
# Outputs
################################################################################

output "state_bucket_name" {
  description = "Name of the S3 bucket for Terraform state"
  value       = aws_s3_bucket.tf_state.id
}

output "state_bucket_arn" {
  description = "ARN of the S3 bucket for Terraform state"
  value       = aws_s3_bucket.tf_state.arn
}

output "state_bucket_region" {
  description = "Region of the S3 bucket for Terraform state"
  value       = var.region
}

output "backend_config" {
  description = "Backend configuration to use in other Terraform configurations (Terraform 1.10+ with S3 native locking)"
  value = {
    bucket       = aws_s3_bucket.tf_state.id
    region       = var.region
    encrypt      = true
    use_lockfile = true
  }
}

output "backend_config_hcl" {
  description = "Example backend configuration block for terraform files"
  value       = <<-EOT
    terraform {
      backend "s3" {
        bucket       = "${aws_s3_bucket.tf_state.id}"
        key          = "<environment>/<region>/terraform.tfstate"
        region       = "${var.region}"
        encrypt      = true
        use_lockfile = true
      }
    }
  EOT
}
