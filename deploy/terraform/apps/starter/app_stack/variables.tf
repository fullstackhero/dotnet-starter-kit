################################################################################
# General Variables
################################################################################

variable "environment" {
  type        = string
  description = "Environment name (dev, staging, prod)."

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "region" {
  type        = string
  description = "AWS region."

  validation {
    condition     = can(regex("^[a-z]{2}-[a-z]+-\\d$", var.region))
    error_message = "Region must be a valid AWS region identifier (e.g., us-east-1)."
  }
}

variable "owner" {
  type        = string
  description = "Owner or team responsible for this infrastructure (used in tags for cost allocation and auditing)."
  default     = null
}

variable "domain_name" {
  type        = string
  description = "Domain name for the application (optional)."
  default     = null
}

################################################################################
# Network Variables
################################################################################

variable "vpc_cidr_block" {
  type        = string
  description = "CIDR block for the VPC."

  validation {
    condition     = can(cidrnetmask(var.vpc_cidr_block))
    error_message = "VPC CIDR block must be a valid CIDR notation."
  }
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
  type        = bool
  description = "Enable NAT Gateway for private subnets."
  default     = true
}

variable "single_nat_gateway" {
  type        = bool
  description = "Use a single NAT Gateway (cost savings for non-prod)."
  default     = true
}

variable "enable_s3_endpoint" {
  type        = bool
  description = "Enable S3 VPC Gateway Endpoint."
  default     = true
}

variable "enable_ecr_endpoints" {
  type        = bool
  description = "Enable ECR VPC Interface Endpoints."
  default     = true
}

variable "enable_logs_endpoint" {
  type        = bool
  description = "Enable CloudWatch Logs VPC Interface Endpoint."
  default     = true
}

variable "enable_secretsmanager_endpoint" {
  type        = bool
  description = "Enable Secrets Manager VPC Interface Endpoint."
  default     = false
}

variable "enable_flow_logs" {
  type        = bool
  description = "Enable VPC Flow Logs."
  default     = false
}

variable "flow_logs_retention_days" {
  type        = number
  description = "Flow logs retention period in days."
  default     = 14
}

################################################################################
# ECS Cluster Variables
################################################################################

variable "enable_container_insights" {
  type        = bool
  description = "Enable Container Insights for ECS cluster."
  default     = true
}

################################################################################
# ALB Variables
################################################################################

variable "enable_https" {
  type        = bool
  description = "Enable HTTPS on the ALB."
  default     = false
}

variable "acm_certificate_arn" {
  type        = string
  description = "ACM certificate ARN for HTTPS (required if enable_https is true)."
  default     = null
}

variable "ssl_policy" {
  type        = string
  description = "SSL policy for the HTTPS listener."
  default     = "ELBSecurityPolicy-TLS13-1-2-2021-06"
}

variable "alb_enable_deletion_protection" {
  type        = bool
  description = "Enable deletion protection for the ALB."
  default     = false
}

variable "alb_idle_timeout" {
  type        = number
  description = "ALB idle timeout in seconds."
  default     = 60
}

variable "alb_access_logs_bucket" {
  type        = string
  description = "S3 bucket for ALB access logs."
  default     = null
}

variable "alb_access_logs_prefix" {
  type        = string
  description = "S3 prefix for ALB access logs."
  default     = "alb-logs"
}

################################################################################
# S3 Variables
################################################################################

variable "app_s3_bucket_name" {
  type        = string
  description = "S3 bucket name for application data (must be globally unique)."

  validation {
    condition     = can(regex("^[a-z0-9][a-z0-9.-]*[a-z0-9]$", var.app_s3_bucket_name))
    error_message = "Bucket name must contain only lowercase letters, numbers, hyphens, and periods."
  }
}

variable "app_s3_versioning_enabled" {
  type        = bool
  description = "Enable versioning on the S3 bucket."
  default     = true
}

variable "app_s3_enable_public_read" {
  type        = bool
  description = "Whether to enable public read on uploads prefix."
  default     = false
}

variable "app_s3_public_read_prefix" {
  type        = string
  description = "Prefix to allow public read (e.g., uploads/)."
  default     = "uploads/"
}

variable "app_s3_enable_cloudfront" {
  type        = bool
  description = "Whether to provision a CloudFront distribution for the app bucket."
  default     = true
}

variable "app_s3_cloudfront_price_class" {
  type        = string
  description = "Price class for CloudFront."
  default     = "PriceClass_100"
}

variable "app_s3_cloudfront_aliases" {
  type        = list(string)
  description = "Alternative domain names (CNAMEs) for CloudFront."
  default     = []
}

variable "app_s3_cloudfront_certificate_arn" {
  type        = string
  description = "ACM certificate ARN for CloudFront (required if using aliases)."
  default     = null
}

variable "app_s3_enable_intelligent_tiering" {
  type        = bool
  description = "Enable automatic transition to Intelligent-Tiering."
  default     = false
}

variable "app_s3_lifecycle_rules" {
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
  description = "List of lifecycle rules for the S3 bucket."
  default     = []
}

################################################################################
# Database Variables
################################################################################

variable "db_name" {
  type        = string
  description = "Database name."
}

variable "db_username" {
  type        = string
  description = "Database admin username."
}

variable "db_password" {
  type        = string
  description = "Database admin password (not used if manage_master_user_password is true)."
  sensitive   = true
  default     = null
}

variable "db_manage_master_user_password" {
  type        = bool
  description = "Let AWS manage the master user password in Secrets Manager."
  default     = false
}

variable "db_instance_class" {
  type        = string
  description = "RDS instance class. Use Graviton (t4g) for best price-performance."
  default     = "db.t4g.micro"
}

variable "db_allocated_storage" {
  type        = number
  description = "Allocated storage in GB."
  default     = 20
}

variable "db_max_allocated_storage" {
  type        = number
  description = "Maximum allocated storage for autoscaling in GB."
  default     = 100
}

variable "db_storage_type" {
  type        = string
  description = "Storage type (gp2, gp3, io1)."
  default     = "gp3"
}

variable "db_engine_version" {
  type        = string
  description = "PostgreSQL engine version."
  default     = "17"
}

variable "db_multi_az" {
  type        = bool
  description = "Enable Multi-AZ deployment."
  default     = false
}

variable "db_backup_retention_period" {
  type        = number
  description = "Backup retention period in days."
  default     = 7
}

variable "db_deletion_protection" {
  type        = bool
  description = "Enable deletion protection."
  default     = false
}

variable "db_enable_performance_insights" {
  type        = bool
  description = "Enable Performance Insights."
  default     = false
}

variable "db_enable_enhanced_monitoring" {
  type        = bool
  description = "Enable Enhanced Monitoring."
  default     = false
}

variable "db_monitoring_interval" {
  type        = number
  description = "Enhanced Monitoring interval in seconds."
  default     = 60
}

variable "db_create_parameter_group" {
  type        = bool
  description = "Create a custom PostgreSQL parameter group with production-tuned settings."
  default     = false
}

variable "db_parameters" {
  type = list(object({
    name         = string
    value        = string
    apply_method = optional(string, "immediate")
  }))
  description = "Custom PostgreSQL parameters."
  default     = []
}

################################################################################
# Redis Variables
################################################################################

variable "redis_node_type" {
  type        = string
  description = "ElastiCache node type. Use Graviton (t4g) for best price-performance."
  default     = "cache.t4g.micro"
}

variable "redis_num_cache_clusters" {
  type        = number
  description = "Number of cache clusters (nodes)."
  default     = 1
}

variable "redis_engine_version" {
  type        = string
  description = "Redis engine version."
  default     = "7.2"
}

variable "redis_automatic_failover_enabled" {
  type        = bool
  description = "Enable automatic failover (requires num_cache_clusters >= 2)."
  default     = false
}

variable "redis_transit_encryption_enabled" {
  type        = bool
  description = "Enable in-transit encryption."
  default     = true
}

################################################################################
# WAF Variables
################################################################################

variable "enable_waf" {
  type        = bool
  description = "Enable AWS WAF for ALB protection."
  default     = true
}

variable "waf_rate_limit" {
  type        = number
  description = "Maximum requests per 5-minute period per IP."
  default     = 2000
}

variable "waf_enable_sqli_rule_set" {
  type        = bool
  description = "Enable SQL injection protection."
  default     = true
}

variable "waf_enable_ip_reputation_rule_set" {
  type        = bool
  description = "Enable IP reputation protection."
  default     = true
}

variable "waf_enable_anonymous_ip_rule_set" {
  type        = bool
  description = "Enable anonymous IP blocking."
  default     = false
}

variable "waf_enable_linux_rule_set" {
  type        = bool
  description = "Enable Linux OS protection rules."
  default     = true
}

variable "waf_enable_logging" {
  type        = bool
  description = "Enable WAF logging to CloudWatch."
  default     = true
}

################################################################################
# CloudWatch Alarms Variables
################################################################################

variable "enable_alarms" {
  type        = bool
  description = "Enable CloudWatch alarms for ECS, RDS, Redis, and ALB."
  default     = false
}

variable "alarm_email_addresses" {
  type        = list(string)
  description = "Email addresses for alarm notifications via SNS."
  default     = []
}

################################################################################
# Container Image Variables
################################################################################

variable "container_registry" {
  type        = string
  description = "Container registry URL (e.g., ghcr.io/fullstackhero)."
  default     = "ghcr.io/fullstackhero"
}

variable "container_image_tag" {
  type        = string
  description = "Container image tag (shared across all services). Must be an immutable tag (git SHA or semver)."

  validation {
    condition     = var.container_image_tag != "latest" && var.container_image_tag != "" && !can(regex("^(dev|staging|prod|main|master)$", var.container_image_tag))
    error_message = "Container image tag must be an immutable identifier (git SHA or semver). Mutable tags like 'latest', 'dev', 'staging', 'prod', 'main', 'master' are not allowed."
  }
}

variable "api_image_name" {
  type        = string
  description = "API container image name (without registry or tag)."
  default     = "fsh-api"
}

################################################################################
# API Service Variables
################################################################################

variable "api_container_port" {
  type        = number
  description = "API container port."
  default     = 8080
}

variable "api_cpu" {
  type        = string
  description = "API CPU units."
  default     = "256"
}

variable "api_memory" {
  type        = string
  description = "API memory."
  default     = "512"
}

variable "api_desired_count" {
  type        = number
  description = "Desired API task count."
  default     = 1
}

variable "api_health_check_healthy_threshold" {
  type        = number
  description = "Number of consecutive health checks required for healthy status."
  default     = 2
}

variable "api_deregistration_delay" {
  type        = number
  description = "Target group deregistration delay in seconds."
  default     = 30
}

variable "api_enable_circuit_breaker" {
  type        = bool
  description = "Enable deployment circuit breaker."
  default     = true
}

variable "api_use_fargate_spot" {
  type        = bool
  description = "Use Fargate Spot capacity."
  default     = false
}

variable "api_enable_autoscaling" {
  type        = bool
  description = "Enable auto-scaling for the API service."
  default     = false
}

variable "api_autoscaling_min_capacity" {
  type        = number
  description = "Minimum number of API tasks when auto-scaling."
  default     = 1
}

variable "api_autoscaling_max_capacity" {
  type        = number
  description = "Maximum number of API tasks when auto-scaling."
  default     = 10
}

variable "api_autoscaling_cpu_target" {
  type        = number
  description = "Target CPU utilization percentage for API auto-scaling."
  default     = 70
}

variable "api_extra_environment_variables" {
  type        = map(string)
  description = "Additional environment variables for API."
  default     = {}
}

