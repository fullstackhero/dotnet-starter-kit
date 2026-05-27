################################################################################
# Required Variables
################################################################################

variable "name" {
  type        = string
  description = "Name identifier for the RDS instance."

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]*$", var.name))
    error_message = "Name must start with a letter and contain only lowercase letters, numbers, and hyphens."
  }
}

variable "vpc_id" {
  type        = string
  description = "VPC ID for RDS."
}

variable "subnet_ids" {
  type        = list(string)
  description = "Subnets for RDS subnet group."

  validation {
    condition     = length(var.subnet_ids) >= 2
    error_message = "At least two subnets in different AZs are required."
  }
}

variable "allowed_security_group_ids" {
  type        = list(string)
  description = "Security groups allowed to access RDS (use when SG IDs are known at plan time)."
  default     = []
}

variable "allowed_cidr_blocks" {
  type        = list(string)
  description = "CIDR blocks allowed to access RDS (use when security groups are not yet created)."
  default     = []
}

variable "vpc_cidr_block" {
  type        = string
  description = "VPC CIDR block (used to restrict egress to VPC only)."
}

variable "db_name" {
  type        = string
  description = "Database name."

  validation {
    condition     = can(regex("^[a-zA-Z][a-zA-Z0-9_]*$", var.db_name))
    error_message = "Database name must start with a letter and contain only alphanumeric characters and underscores."
  }
}

variable "username" {
  type        = string
  description = "Database admin username."
  sensitive   = true

  validation {
    condition     = can(regex("^[a-zA-Z][a-zA-Z0-9_]*$", var.username))
    error_message = "Username must start with a letter and contain only alphanumeric characters and underscores."
  }
}

################################################################################
# Password Options
################################################################################

variable "password" {
  type        = string
  description = "Database admin password. Required if manage_master_user_password is false."
  default     = null
  sensitive   = true
}

variable "manage_master_user_password" {
  type        = bool
  description = "Use AWS Secrets Manager to manage the master password."
  default     = false
}

################################################################################
# Engine Configuration
################################################################################

variable "engine_version" {
  type        = string
  description = "PostgreSQL engine version."
  default     = "17"
}

variable "instance_class" {
  type        = string
  description = "RDS instance class."
  default     = "db.t4g.micro"
}

################################################################################
# Storage Configuration
################################################################################

variable "allocated_storage" {
  type        = number
  description = "Allocated storage in GB."
  default     = 20

  validation {
    condition     = var.allocated_storage >= 20
    error_message = "Allocated storage must be at least 20 GB."
  }
}

variable "max_allocated_storage" {
  type        = number
  description = "Maximum allocated storage for autoscaling (0 to disable)."
  default     = 100
}

variable "storage_type" {
  type        = string
  description = "Storage type (gp2, gp3, io1, io2)."
  default     = "gp3"

  validation {
    condition     = contains(["gp2", "gp3", "io1", "io2"], var.storage_type)
    error_message = "Storage type must be gp2, gp3, io1, or io2."
  }
}

variable "iops" {
  type        = number
  description = "Provisioned IOPS (for io1/io2 storage)."
  default     = null
}

variable "kms_key_id" {
  type        = string
  description = "KMS key ID for storage encryption. Uses default AWS key if not specified."
  default     = null
}

################################################################################
# Backup Configuration
################################################################################

variable "backup_retention_period" {
  type        = number
  description = "Backup retention period in days."
  default     = 7

  validation {
    condition     = var.backup_retention_period >= 0 && var.backup_retention_period <= 35
    error_message = "Backup retention period must be between 0 and 35 days."
  }
}

variable "backup_window" {
  type        = string
  description = "Preferred backup window (UTC)."
  default     = "03:00-04:00"
}

variable "maintenance_window" {
  type        = string
  description = "Preferred maintenance window (UTC)."
  default     = "sun:05:00-sun:06:00"
}

variable "skip_final_snapshot" {
  type        = bool
  description = "Skip final snapshot on destroy."
  default     = false
}

variable "final_snapshot_identifier" {
  type        = string
  description = "Name of the final snapshot. Auto-generated from instance name if not specified."
  default     = null
}

variable "delete_automated_backups" {
  type        = bool
  description = "Delete automated backups on instance deletion."
  default     = true
}

################################################################################
# High Availability Configuration
################################################################################

variable "multi_az" {
  type        = bool
  description = "Enable Multi-AZ deployment for high availability."
  default     = false
}

################################################################################
# Monitoring Configuration
################################################################################

variable "performance_insights_enabled" {
  type        = bool
  description = "Enable Performance Insights."
  default     = true
}

variable "performance_insights_retention_period" {
  type        = number
  description = "Performance Insights retention period (7 or 731 days)."
  default     = 7

  validation {
    condition     = contains([7, 731], var.performance_insights_retention_period)
    error_message = "Performance Insights retention must be 7 or 731 days."
  }
}

variable "monitoring_interval" {
  type        = number
  description = "Enhanced Monitoring interval in seconds (0 to disable, 1, 5, 10, 15, 30, or 60)."
  default     = 0

  validation {
    condition     = contains([0, 1, 5, 10, 15, 30, 60], var.monitoring_interval)
    error_message = "Monitoring interval must be 0, 1, 5, 10, 15, 30, or 60 seconds."
  }
}

################################################################################
# Upgrade Configuration
################################################################################

variable "auto_minor_version_upgrade" {
  type        = bool
  description = "Enable automatic minor version upgrades."
  default     = true
}

variable "allow_major_version_upgrade" {
  type        = bool
  description = "Allow major version upgrades."
  default     = false
}

variable "apply_immediately" {
  type        = bool
  description = "Apply changes immediately instead of during maintenance window."
  default     = false
}

################################################################################
# IAM Authentication
################################################################################

variable "iam_database_authentication_enabled" {
  type        = bool
  description = "Enable IAM database authentication for passwordless access."
  default     = false
}

################################################################################
# CloudWatch Log Exports
################################################################################

variable "cloudwatch_log_exports" {
  type        = list(string)
  description = "List of log types to export to CloudWatch (postgresql, upgrade)."
  default     = ["postgresql"]

  validation {
    condition     = alltrue([for log in var.cloudwatch_log_exports : contains(["postgresql", "upgrade"], log)])
    error_message = "Valid log types are: postgresql, upgrade."
  }
}

################################################################################
# Protection Configuration
################################################################################

variable "deletion_protection" {
  type        = bool
  description = "Enable deletion protection."
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
    name         = string
    value        = string
    apply_method = optional(string, "immediate")
  }))
  description = "List of DB parameters to apply."
  default     = []
}

################################################################################
# Tags
################################################################################

variable "tags" {
  type        = map(string)
  description = "Tags to apply to RDS resources."
  default     = {}
}
