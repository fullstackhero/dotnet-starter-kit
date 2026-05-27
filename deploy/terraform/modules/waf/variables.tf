################################################################################
# Required Variables
################################################################################

variable "name" {
  type        = string
  description = "Name of the WAF Web ACL."

  validation {
    condition     = can(regex("^[a-zA-Z0-9-]+$", var.name))
    error_message = "WAF name must contain only alphanumeric characters and hyphens."
  }
}

################################################################################
# Association
################################################################################

variable "alb_arn" {
  type        = string
  description = "ARN of the ALB to associate with the WAF. Set to null to skip association."
  default     = null
}

################################################################################
# Rule Configuration
################################################################################

variable "description" {
  type        = string
  description = "Description of the WAF Web ACL."
  default     = ""
}

variable "rate_limit" {
  type        = number
  description = "Maximum number of requests per 5-minute period per IP address."
  default     = 2000

  validation {
    condition     = var.rate_limit >= 100 && var.rate_limit <= 20000000
    error_message = "Rate limit must be between 100 and 20,000,000."
  }
}

variable "common_ruleset_excluded_rules" {
  type        = list(string)
  description = "List of rule names to exclude (count instead of block) from the Common Rule Set."
  default     = []
}

variable "enable_sqli_rule_set" {
  type        = bool
  description = "Enable AWS Managed SQL Injection rule set."
  default     = true
}

variable "enable_ip_reputation_rule_set" {
  type        = bool
  description = "Enable AWS Managed IP Reputation rule set."
  default     = true
}

variable "enable_anonymous_ip_rule_set" {
  type        = bool
  description = "Enable AWS Managed Anonymous IP List rule set."
  default     = false
}

variable "enable_linux_rule_set" {
  type        = bool
  description = "Enable AWS Managed Linux OS rule set."
  default     = true
}

################################################################################
# Logging
################################################################################

variable "enable_logging" {
  type        = bool
  description = "Enable WAF logging to CloudWatch Logs."
  default     = true
}

variable "log_retention_in_days" {
  type        = number
  description = "CloudWatch log retention in days."
  default     = 30

  validation {
    condition     = contains([0, 1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1096, 1827, 2192, 2557, 2922, 3288, 3653], var.log_retention_in_days)
    error_message = "Log retention must be a valid CloudWatch Logs retention value."
  }
}

variable "kms_key_id" {
  type        = string
  description = "KMS key ID for log group encryption."
  default     = null
}

variable "redacted_fields" {
  type = list(object({
    type = string
    name = string
  }))
  description = "Fields to redact from WAF logs (e.g., Authorization header)."
  default = [
    {
      type = "single_header"
      name = "authorization"
    },
    {
      type = "single_header"
      name = "cookie"
    }
  ]
}

################################################################################
# Tags
################################################################################

variable "tags" {
  type        = map(string)
  description = "Tags to apply to WAF resources."
  default     = {}
}
