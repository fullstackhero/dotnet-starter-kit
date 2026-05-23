################################################################################
# Required
################################################################################

variable "name" {
  type        = string
  description = "S3 bucket name for the static site (must be globally unique)."

  validation {
    condition     = can(regex("^[a-z0-9][a-z0-9.-]*[a-z0-9]$", var.name))
    error_message = "Bucket name must contain only lowercase letters, numbers, hyphens, and periods."
  }
}

################################################################################
# S3
################################################################################

variable "force_destroy" {
  type        = bool
  description = "Allow Terraform to destroy the bucket even if it contains objects (use only for non-prod)."
  default     = false
}

variable "versioning_enabled" {
  type        = bool
  description = "Enable S3 object versioning. SPA artifacts are immutable per release, so this is off by default."
  default     = false
}

variable "kms_key_arn" {
  type        = string
  description = "KMS key ARN for bucket SSE. Null uses SSE-S3 (AES256)."
  default     = null
}

################################################################################
# CloudFront
################################################################################

variable "price_class" {
  type        = string
  description = "CloudFront price class (PriceClass_100, PriceClass_200, PriceClass_All)."
  default     = "PriceClass_100"

  validation {
    condition     = contains(["PriceClass_100", "PriceClass_200", "PriceClass_All"], var.price_class)
    error_message = "Price class must be PriceClass_100, PriceClass_200, or PriceClass_All."
  }
}

variable "default_root_object" {
  type        = string
  description = "Object returned for requests to the distribution root."
  default     = "index.html"
}

variable "aliases" {
  type        = list(string)
  description = "Custom domain names (CNAMEs) served by this distribution. Requires acm_certificate_arn."
  default     = []
}

variable "acm_certificate_arn" {
  type        = string
  description = "ACM certificate ARN for the custom domains. MUST be issued in us-east-1 for CloudFront. Null uses the default *.cloudfront.net certificate."
  default     = null

  validation {
    condition     = var.acm_certificate_arn == null || can(regex("^arn:aws:acm:us-east-1:", coalesce(var.acm_certificate_arn, "arn:aws:acm:us-east-1:")))
    error_message = "CloudFront ACM certificates must be issued in us-east-1."
  }
}

variable "minimum_protocol_version" {
  type        = string
  description = "Minimum TLS version when a custom certificate is used."
  default     = "TLSv1.2_2021"
}

variable "spa_fallback_enabled" {
  type        = bool
  description = "Rewrite 403/404 origin responses to the SPA entry point with a 200 (client-side routing)."
  default     = true
}

variable "spa_fallback_path" {
  type        = string
  description = "Path returned for the SPA fallback (client-side router entry point)."
  default     = "/index.html"
}

variable "cache_policy_name" {
  type        = string
  description = "Managed CloudFront cache policy name for static assets."
  default     = "Managed-CachingOptimized"
}

variable "response_headers_policy_id" {
  type        = string
  description = "Explicit CloudFront response headers policy ID. Overrides security_headers_enabled when set."
  default     = null
}

variable "security_headers_enabled" {
  type        = bool
  description = "Attach a managed response headers policy adding HSTS, X-Content-Type-Options, X-Frame-Options, and Referrer-Policy. Ignored when response_headers_policy_id is set."
  default     = true
}

variable "response_headers_policy_name" {
  type        = string
  description = "Managed response headers policy name used when security_headers_enabled is true and no explicit ID is provided."
  default     = "Managed-SecurityHeadersPolicy"
}

variable "geo_restriction_type" {
  type        = string
  description = "Geo restriction type (none, whitelist, blacklist)."
  default     = "none"

  validation {
    condition     = contains(["none", "whitelist", "blacklist"], var.geo_restriction_type)
    error_message = "Geo restriction type must be none, whitelist, or blacklist."
  }
}

variable "geo_restriction_locations" {
  type        = list(string)
  description = "ISO 3166-1-alpha-2 country codes for the geo restriction."
  default     = []
}

variable "comment" {
  type        = string
  description = "Comment shown on the CloudFront distribution."
  default     = ""
}

################################################################################
# Runtime configuration (config.json)
################################################################################

variable "runtime_config" {
  type        = any
  description = "If set, Terraform writes this object as a JSON file (runtime_config_object_key) so the SPA can read environment settings at boot without a rebuild. Served with no caching."
  default     = null
}

variable "runtime_config_object_key" {
  type        = string
  description = "Object key for the runtime config JSON file."
  default     = "config.json"
}

################################################################################
# Tagging
################################################################################

variable "tags" {
  type        = map(string)
  description = "Tags applied to all resources."
  default     = {}
}
