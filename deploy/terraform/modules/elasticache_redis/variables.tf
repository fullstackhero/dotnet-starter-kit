################################################################################
# Required Variables
################################################################################

variable "name" {
  type        = string
  description = "Name identifier for the Redis replication group."

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]*$", var.name))
    error_message = "Name must start with a letter and contain only lowercase letters, numbers, and hyphens."
  }
}

variable "vpc_id" {
  type        = string
  description = "VPC ID."
}

variable "subnet_ids" {
  type        = list(string)
  description = "Subnets for ElastiCache."

  validation {
    condition     = length(var.subnet_ids) >= 1
    error_message = "At least one subnet must be specified."
  }
}

variable "allowed_security_group_ids" {
  type        = list(string)
  description = "Security groups allowed to access Redis (use when SG IDs are known at plan time)."
  default     = []
}

variable "allowed_cidr_blocks" {
  type        = list(string)
  description = "CIDR blocks allowed to access Redis (use when security groups are not yet created)."
  default     = []
}

variable "vpc_cidr_block" {
  type        = string
  description = "VPC CIDR block (used to restrict egress to VPC only)."
}

################################################################################
# Engine Configuration
################################################################################

variable "engine_version" {
  type        = string
  description = "Redis engine version."
  default     = "7.2"
}

variable "node_type" {
  type        = string
  description = "Node instance type."
  default     = "cache.t4g.micro"
}

variable "description" {
  type        = string
  description = "Description of the replication group."
  default     = ""
}

################################################################################
# Cluster Configuration
################################################################################

variable "num_cache_clusters" {
  type        = number
  description = "Number of cache clusters (nodes)."
  default     = 1

  validation {
    condition     = var.num_cache_clusters >= 1 && var.num_cache_clusters <= 6
    error_message = "Number of cache clusters must be between 1 and 6."
  }
}

variable "automatic_failover_enabled" {
  type        = bool
  description = "Enable automatic failover (requires num_cache_clusters >= 2)."
  default     = false
}

variable "multi_az_enabled" {
  type        = bool
  description = "Enable Multi-AZ (requires automatic_failover_enabled)."
  default     = false
}

################################################################################
# Security Configuration
################################################################################

variable "transit_encryption_enabled" {
  type        = bool
  description = "Enable encryption in transit."
  default     = true
}

variable "auth_token" {
  type        = string
  description = "Auth token for Redis AUTH (requires transit_encryption_enabled)."
  default     = null
  sensitive   = true
}

variable "kms_key_id" {
  type        = string
  description = "KMS key ID for at-rest encryption. Uses default AWS key if not specified."
  default     = null
}

################################################################################
# Maintenance Configuration
################################################################################

variable "auto_minor_version_upgrade" {
  type        = bool
  description = "Enable automatic minor version upgrades."
  default     = true
}

variable "apply_immediately" {
  type        = bool
  description = "Apply changes immediately instead of during maintenance window."
  default     = false
}

variable "maintenance_window" {
  type        = string
  description = "Maintenance window (UTC)."
  default     = "sun:05:00-sun:06:00"
}

################################################################################
# Snapshot Configuration
################################################################################

variable "snapshot_retention_limit" {
  type        = number
  description = "Days to retain snapshots (0 to disable)."
  default     = 7

  validation {
    condition     = var.snapshot_retention_limit >= 0 && var.snapshot_retention_limit <= 35
    error_message = "Snapshot retention must be between 0 and 35 days."
  }
}

variable "snapshot_window" {
  type        = string
  description = "Daily snapshot window (UTC)."
  default     = "03:00-04:00"
}

variable "skip_final_snapshot" {
  type        = bool
  description = "Skip final snapshot on destroy."
  default     = false
}

################################################################################
# Parameter Group Configuration
################################################################################

variable "create_parameter_group" {
  type        = bool
  description = "Create a custom parameter group."
  default     = false
}

variable "parameters" {
  type = list(object({
    name  = string
    value = string
  }))
  description = "List of Redis parameters to apply."
  default     = []
}

################################################################################
# Logging
################################################################################

variable "enable_slow_log" {
  type        = bool
  description = "Enable slow log delivery to CloudWatch Logs."
  default     = false
}

variable "enable_engine_log" {
  type        = bool
  description = "Enable engine log delivery to CloudWatch Logs."
  default     = false
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

################################################################################
# Notifications
################################################################################

variable "notification_topic_arn" {
  type        = string
  description = "SNS topic ARN for notifications."
  default     = null
}

################################################################################
# Tags
################################################################################

variable "tags" {
  type        = map(string)
  description = "Tags to apply to Redis resources."
  default     = {}
}
