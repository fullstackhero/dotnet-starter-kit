################################################################################
# Required Variables
################################################################################

variable "name" {
  type        = string
  description = "Name prefix for alarm resources."

  validation {
    condition     = can(regex("^[a-zA-Z0-9-]+$", var.name))
    error_message = "Name must contain only alphanumeric characters and hyphens."
  }
}

################################################################################
# Notification Configuration
################################################################################

variable "alarm_email_addresses" {
  type        = list(string)
  description = "Email addresses for alarm notifications."
  default     = []
}

variable "kms_key_id" {
  type        = string
  description = "KMS key ID for SNS topic encryption."
  default     = null
}

################################################################################
# ECS Configuration
################################################################################

variable "ecs_services" {
  type = map(object({
    cluster_name = string
    service_name = string
  }))
  description = "Map of ECS services to monitor."
  default     = {}
}

variable "ecs_cpu_threshold" {
  type        = number
  description = "CPU utilization threshold for ECS alarms."
  default     = 85
}

variable "ecs_memory_threshold" {
  type        = number
  description = "Memory utilization threshold for ECS alarms."
  default     = 85
}

################################################################################
# RDS Configuration
################################################################################

variable "rds_instance_identifier" {
  type        = string
  description = "RDS instance identifier to monitor."
  default     = null
}

variable "rds_cpu_threshold" {
  type        = number
  description = "CPU utilization threshold for RDS alarms."
  default     = 80
}

variable "rds_free_storage_threshold_gb" {
  type        = number
  description = "Free storage threshold in GB."
  default     = 5
}

variable "rds_connections_threshold" {
  type        = number
  description = "Database connection count threshold."
  default     = 100
}

variable "rds_read_latency_threshold_ms" {
  type        = number
  description = "Read latency threshold in milliseconds."
  default     = 20
}

################################################################################
# ElastiCache Redis Configuration
################################################################################

variable "redis_replication_group_id" {
  type        = string
  description = "ElastiCache replication group ID to monitor."
  default     = null
}

variable "redis_cpu_threshold" {
  type        = number
  description = "Engine CPU utilization threshold for Redis alarms."
  default     = 75
}

variable "redis_memory_threshold" {
  type        = number
  description = "Memory usage percentage threshold for Redis alarms."
  default     = 80
}

variable "redis_evictions_threshold" {
  type        = number
  description = "Evictions threshold for Redis alarms."
  default     = 100
}

################################################################################
# ALB Configuration
################################################################################

variable "alb_arn_suffix" {
  type        = string
  description = "ALB ARN suffix to monitor."
  default     = null
}

variable "alb_5xx_threshold" {
  type        = number
  description = "ALB 5xx error count threshold."
  default     = 10
}

variable "alb_target_5xx_threshold" {
  type        = number
  description = "ALB target 5xx error count threshold."
  default     = 25
}

variable "alb_response_time_threshold" {
  type        = number
  description = "ALB target response time threshold in seconds."
  default     = 2
}

variable "alb_target_group_arns" {
  type = map(object({
    target_group_arn_suffix = string
  }))
  description = "Map of target groups to monitor for unhealthy hosts."
  default     = {}
}

################################################################################
# Tags
################################################################################

variable "tags" {
  type        = map(string)
  description = "Tags to apply to alarm resources."
  default     = {}
}
