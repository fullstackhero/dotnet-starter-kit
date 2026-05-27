variable "region" {
  type        = string
  description = "AWS region where the state bucket is created."

  validation {
    condition     = can(regex("^[a-z]{2}-[a-z]+-\\d$", var.region))
    error_message = "Region must be a valid AWS region identifier (e.g., us-east-1)."
  }
}

variable "bucket_name" {
  type        = string
  description = "Name of the S3 bucket for Terraform remote state (must be globally unique)."

  validation {
    condition     = can(regex("^[a-z0-9][a-z0-9.-]*[a-z0-9]$", var.bucket_name))
    error_message = "Bucket name must contain only lowercase letters, numbers, hyphens, and periods."
  }
}

variable "kms_key_arn" {
  type        = string
  description = "KMS key ARN for encryption. Uses AWS-managed key if not specified."
  default     = null
}

variable "state_version_retention_days" {
  type        = number
  description = "Number of days to retain non-current state file versions."
  default     = 90

  validation {
    condition     = var.state_version_retention_days >= 1
    error_message = "State version retention must be at least 1 day."
  }
}
