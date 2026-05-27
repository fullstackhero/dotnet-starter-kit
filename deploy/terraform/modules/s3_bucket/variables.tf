################################################################################
# Required Variables
################################################################################

variable "name" {
  type        = string
  description = "Bucket name (must be globally unique)."

  validation {
    condition     = can(regex("^[a-z0-9][a-z0-9.-]*[a-z0-9]$", var.name))
    error_message = "Bucket name must contain only lowercase letters, numbers, hyphens, and periods, and must start and end with a letter or number."
  }
}

################################################################################
# Bucket Configuration
################################################################################

variable "force_destroy" {
  type        = bool
  description = "Allow bucket destruction even if not empty."
  default     = false
}

variable "versioning_enabled" {
  type        = bool
  description = "Enable versioning."
  default     = true
}

variable "kms_key_arn" {
  type        = string
  description = "KMS key ARN for server-side encryption. Uses AES256 if not specified."
  default     = null
}

################################################################################
# Public Access Configuration
################################################################################

variable "enable_public_read" {
  type        = bool
  description = "Set to true to allow public read on the specified prefix via bucket policy."
  default     = false
}

variable "public_read_prefix" {
  type        = string
  description = "Prefix to allow public read (e.g., uploads/). Leave empty to disable public policy."
  default     = "uploads/"
}

################################################################################
# Lifecycle Rules
################################################################################

variable "enable_intelligent_tiering" {
  type        = bool
  description = "Enable automatic transition to Intelligent-Tiering."
  default     = false
}

variable "lifecycle_rules" {
  type = list(object({
    id                                     = string
    enabled                                = optional(bool, true)
    prefix                                 = optional(string, "")
    expiration_days                        = optional(number)
    noncurrent_version_expiration_days     = optional(number)
    abort_incomplete_multipart_upload_days = optional(number, 7)
    transitions = optional(list(object({
      days          = number
      storage_class = string
    })), [])
    noncurrent_version_transitions = optional(list(object({
      days          = number
      storage_class = string
    })), [])
  }))
  description = "List of lifecycle rules."
  default     = []
}

################################################################################
# CORS Configuration
################################################################################

variable "cors_rules" {
  type = list(object({
    allowed_headers = optional(list(string), ["*"])
    allowed_methods = list(string)
    allowed_origins = list(string)
    expose_headers  = optional(list(string), [])
    max_age_seconds = optional(number, 3000)
  }))
  description = "List of CORS rules."
  default     = []
}

################################################################################
# CloudFront Configuration
################################################################################

variable "enable_cloudfront" {
  type        = bool
  description = "Set to true to provision a CloudFront distribution in front of the bucket."
  default     = false
}

variable "cloudfront_price_class" {
  type        = string
  description = "CloudFront price class."
  default     = "PriceClass_100"

  validation {
    condition     = contains(["PriceClass_All", "PriceClass_200", "PriceClass_100"], var.cloudfront_price_class)
    error_message = "Price class must be PriceClass_All, PriceClass_200, or PriceClass_100."
  }
}

variable "cloudfront_comment" {
  type        = string
  description = "Optional comment for the CloudFront distribution."
  default     = ""
}

variable "cloudfront_default_root_object" {
  type        = string
  description = "Default root object for CloudFront."
  default     = ""
}

variable "cloudfront_aliases" {
  type        = list(string)
  description = "Alternative domain names (CNAMEs) for CloudFront."
  default     = []
}

variable "cloudfront_acm_certificate_arn" {
  type        = string
  description = "ACM certificate ARN for CloudFront (required if using aliases)."
  default     = null
}

variable "cloudfront_cache_policy_id" {
  type        = string
  description = "CloudFront cache policy ID. Uses default if not specified."
  default     = null
}

variable "cloudfront_origin_request_policy_id" {
  type        = string
  description = "CloudFront origin request policy ID."
  default     = null
}

variable "cloudfront_geo_restriction_type" {
  type        = string
  description = "CloudFront geo restriction type (none, whitelist, blacklist)."
  default     = "none"

  validation {
    condition     = contains(["none", "whitelist", "blacklist"], var.cloudfront_geo_restriction_type)
    error_message = "Geo restriction type must be none, whitelist, or blacklist."
  }
}

variable "cloudfront_geo_restriction_locations" {
  type        = list(string)
  description = "Country codes for geo restriction."
  default     = []
}

################################################################################
# Additional Bucket Policy
################################################################################

variable "additional_bucket_policy_statements" {
  type        = any
  description = "Additional bucket policy statements."
  default     = []
}

################################################################################
# Tags
################################################################################

variable "tags" {
  type        = map(string)
  description = "Tags to apply to the bucket."
  default     = {}
}
