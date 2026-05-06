################################################################################
# General
################################################################################

variable "environment" {
  type        = string
  description = "Environment name (dev, staging, prod)."
}

variable "region" {
  type        = string
  description = "AWS region."
}

variable "domain_name" {
  type        = string
  description = "Domain name for the application (optional)."
  default     = null
}

variable "owner" {
  type        = string
  description = "Owner or team responsible for this infrastructure."
  default     = null
}

################################################################################
# Network
################################################################################

variable "vpc_cidr_block" {
  type        = string
  description = "CIDR block for the VPC."
}

variable "public_subnets" {
  description = "Public subnet definitions."
  type = map(object({
    cidr_block = string
    az         = string
  }))
}

variable "private_subnets" {
  description = "Private subnet definitions."
  type = map(object({
    cidr_block = string
    az         = string
  }))
}

variable "enable_nat_gateway" {
  type    = bool
  default = true
}

variable "single_nat_gateway" {
  type    = bool
  default = true
}

variable "enable_s3_endpoint" {
  type    = bool
  default = true
}

variable "enable_ecr_endpoints" {
  type    = bool
  default = true
}

variable "enable_logs_endpoint" {
  type    = bool
  default = true
}

variable "enable_secretsmanager_endpoint" {
  type    = bool
  default = false
}

variable "enable_flow_logs" {
  type    = bool
  default = false
}

variable "flow_logs_retention_days" {
  type    = number
  default = 14
}

################################################################################
# ECS
################################################################################

variable "enable_container_insights" {
  type    = bool
  default = false
}

################################################################################
# ALB
################################################################################

variable "enable_https" {
  type    = bool
  default = false
}

variable "acm_certificate_arn" {
  type    = string
  default = null
}

variable "alb_enable_deletion_protection" {
  type    = bool
  default = false
}

################################################################################
# Alarms
################################################################################

variable "enable_alarms" {
  type    = bool
  default = false
}

variable "alarm_email_addresses" {
  type    = list(string)
  default = []
}

################################################################################
# WAF
################################################################################

variable "enable_waf" {
  type    = bool
  default = false
}

variable "waf_rate_limit" {
  type    = number
  default = 2000
}

variable "waf_enable_sqli_rule_set" {
  type    = bool
  default = true
}

variable "waf_enable_ip_reputation_rule_set" {
  type    = bool
  default = true
}

variable "waf_enable_anonymous_ip_rule_set" {
  type    = bool
  default = false
}

variable "waf_enable_linux_rule_set" {
  type    = bool
  default = true
}

variable "waf_enable_logging" {
  type    = bool
  default = true
}

################################################################################
# S3
################################################################################

variable "app_s3_bucket_name" {
  type        = string
  description = "S3 bucket name for application data (must be globally unique)."
}

variable "app_s3_versioning_enabled" {
  type    = bool
  default = true
}

variable "app_s3_enable_public_read" {
  type    = bool
  default = false
}

variable "app_s3_enable_cloudfront" {
  type    = bool
  default = true
}

variable "app_s3_cloudfront_price_class" {
  type    = string
  default = "PriceClass_100"
}

variable "app_s3_enable_intelligent_tiering" {
  type    = bool
  default = false
}

################################################################################
# Database
################################################################################

variable "db_name" {
  type = string
}

variable "db_username" {
  type      = string
  sensitive = true
}

variable "db_password" {
  type      = string
  sensitive = true
  default   = null
}

variable "db_manage_master_user_password" {
  type    = bool
  default = true
}

variable "db_instance_class" {
  type    = string
  default = "db.t4g.micro"
}

variable "db_allocated_storage" {
  type    = number
  default = 20
}

variable "db_max_allocated_storage" {
  type    = number
  default = 100
}

variable "db_engine_version" {
  type    = string
  default = "17"
}

variable "db_multi_az" {
  type    = bool
  default = false
}

variable "db_backup_retention_period" {
  type    = number
  default = 7
}

variable "db_deletion_protection" {
  type    = bool
  default = false
}

variable "db_enable_performance_insights" {
  type    = bool
  default = false
}

variable "db_enable_enhanced_monitoring" {
  type    = bool
  default = false
}

variable "db_create_parameter_group" {
  type    = bool
  default = false
}

variable "db_parameters" {
  type = list(object({
    name         = string
    value        = string
    apply_method = optional(string, "immediate")
  }))
  default = []
}

################################################################################
# Redis
################################################################################

variable "redis_node_type" {
  type    = string
  default = "cache.t4g.micro"
}

variable "redis_num_cache_clusters" {
  type    = number
  default = 1
}

variable "redis_automatic_failover_enabled" {
  type    = bool
  default = false
}

################################################################################
# Container Images
################################################################################

variable "container_registry" {
  type    = string
  default = "ghcr.io/fullstackhero"
}

variable "container_image_tag" {
  type        = string
  description = "Container image tag. Must be immutable (git SHA or semver)."
}

variable "api_image_name" {
  type    = string
  default = "fsh-api"
}

################################################################################
# API Service
################################################################################

variable "api_container_port" {
  type    = number
  default = 8080
}

variable "api_cpu" {
  type    = string
  default = "256"
}

variable "api_memory" {
  type    = string
  default = "512"
}

variable "api_desired_count" {
  type    = number
  default = 1
}

variable "api_enable_circuit_breaker" {
  type    = bool
  default = true
}

variable "api_use_fargate_spot" {
  type    = bool
  default = false
}

variable "api_enable_autoscaling" {
  type    = bool
  default = false
}

variable "api_autoscaling_min_capacity" {
  type    = number
  default = 1
}

variable "api_autoscaling_max_capacity" {
  type    = number
  default = 10
}

variable "api_autoscaling_cpu_target" {
  type    = number
  default = 70
}

