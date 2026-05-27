################################################################################
# Required Variables
################################################################################

variable "name" {
  type        = string
  description = "Name of the ALB."

  validation {
    condition     = can(regex("^[a-zA-Z0-9-]+$", var.name))
    error_message = "ALB name must contain only alphanumeric characters and hyphens."
  }
}

variable "subnet_ids" {
  type        = list(string)
  description = "Subnets for the ALB."

  validation {
    condition     = length(var.subnet_ids) >= 2
    error_message = "At least two subnets are required for ALB."
  }
}

variable "security_group_id" {
  type        = string
  description = "Security group for the ALB."
}

################################################################################
# ALB Configuration
################################################################################

variable "internal" {
  type        = bool
  description = "Whether the ALB is internal."
  default     = false
}

variable "enable_deletion_protection" {
  type        = bool
  description = "Enable deletion protection."
  default     = false
}

variable "enable_http2" {
  type        = bool
  description = "Enable HTTP/2."
  default     = true
}

variable "idle_timeout" {
  type        = number
  description = "Idle timeout in seconds."
  default     = 60

  validation {
    condition     = var.idle_timeout >= 1 && var.idle_timeout <= 4000
    error_message = "Idle timeout must be between 1 and 4000 seconds."
  }
}

variable "drop_invalid_header_fields" {
  type        = bool
  description = "Drop invalid HTTP headers."
  default     = true
}

variable "desync_mitigation_mode" {
  type        = string
  description = "How the ALB handles requests that might pose a security risk due to HTTP desync (monitor, defensive, strictest)."
  default     = "defensive"

  validation {
    condition     = contains(["monitor", "defensive", "strictest"], var.desync_mitigation_mode)
    error_message = "Desync mitigation mode must be monitor, defensive, or strictest."
  }
}

variable "preserve_host_header" {
  type        = bool
  description = "Preserve the Host header in the HTTP request and send it to the target without modification."
  default     = false
}

variable "xff_header_processing_mode" {
  type        = string
  description = "How the X-Forwarded-For header is processed (append, preserve, remove)."
  default     = "append"

  validation {
    condition     = contains(["append", "preserve", "remove"], var.xff_header_processing_mode)
    error_message = "XFF header processing mode must be append, preserve, or remove."
  }
}

################################################################################
# HTTPS Configuration
################################################################################

variable "enable_https" {
  type        = bool
  description = "Enable HTTPS listener (requires certificate_arn)."
  default     = false
}

variable "certificate_arn" {
  type        = string
  description = "ARN of the default SSL certificate."
  default     = null
}

variable "additional_certificate_arns" {
  type        = list(string)
  description = "Additional SSL certificate ARNs for SNI."
  default     = []
}

variable "ssl_policy" {
  type        = string
  description = "SSL policy for HTTPS listener. Use TLS 1.3 policies for best security."
  default     = "ELBSecurityPolicy-TLS13-1-2-2021-06"

  validation {
    condition = contains([
      "ELBSecurityPolicy-TLS13-1-2-2021-06",
      "ELBSecurityPolicy-TLS13-1-3-2021-06",
      "ELBSecurityPolicy-TLS13-1-2-Ext1-2021-06",
      "ELBSecurityPolicy-TLS13-1-2-Ext2-2021-06",
      "ELBSecurityPolicy-FS-1-2-Res-2020-10",
      "ELBSecurityPolicy-FS-1-2-2019-08"
    ], var.ssl_policy)
    error_message = "SSL policy must be a valid ELB security policy supporting TLS 1.2+."
  }
}

################################################################################
# Access Logs
################################################################################

variable "access_logs_bucket" {
  type        = string
  description = "S3 bucket for access logs."
  default     = null
}

variable "access_logs_prefix" {
  type        = string
  description = "S3 prefix for access logs."
  default     = "alb-logs"
}

variable "connection_logs_bucket" {
  type        = string
  description = "S3 bucket for connection logs."
  default     = null
}

variable "connection_logs_prefix" {
  type        = string
  description = "S3 prefix for connection logs."
  default     = "alb-connection-logs"
}

################################################################################
# Tags
################################################################################

variable "tags" {
  type        = map(string)
  description = "Tags to apply to ALB resources."
  default     = {}
}
