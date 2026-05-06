terraform {
  required_version = ">= 1.14.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 5.90.0"
    }
  }
}

provider "aws" {
  region = var.region

  default_tags {
    tags = {
      Environment = var.environment
      Project     = "dotnet-starter-kit"
      ManagedBy   = "terraform"
    }
  }
}

module "app" {
  source = "../app_stack"

  # General
  environment = var.environment
  region      = var.region
  domain_name = var.domain_name
  owner       = var.owner

  # Network
  vpc_cidr_block                 = var.vpc_cidr_block
  public_subnets                 = var.public_subnets
  private_subnets                = var.private_subnets
  enable_nat_gateway             = var.enable_nat_gateway
  single_nat_gateway             = var.single_nat_gateway
  enable_s3_endpoint             = var.enable_s3_endpoint
  enable_ecr_endpoints           = var.enable_ecr_endpoints
  enable_logs_endpoint           = var.enable_logs_endpoint
  enable_secretsmanager_endpoint = var.enable_secretsmanager_endpoint
  enable_flow_logs               = var.enable_flow_logs
  flow_logs_retention_days       = var.flow_logs_retention_days

  # ECS
  enable_container_insights = var.enable_container_insights

  # ALB
  enable_https                   = var.enable_https
  acm_certificate_arn            = var.acm_certificate_arn
  alb_enable_deletion_protection = var.alb_enable_deletion_protection

  # Alarms
  enable_alarms         = var.enable_alarms
  alarm_email_addresses = var.alarm_email_addresses

  # WAF
  enable_waf                        = var.enable_waf
  waf_rate_limit                    = var.waf_rate_limit
  waf_enable_sqli_rule_set          = var.waf_enable_sqli_rule_set
  waf_enable_ip_reputation_rule_set = var.waf_enable_ip_reputation_rule_set
  waf_enable_anonymous_ip_rule_set  = var.waf_enable_anonymous_ip_rule_set
  waf_enable_linux_rule_set         = var.waf_enable_linux_rule_set
  waf_enable_logging                = var.waf_enable_logging

  # S3
  app_s3_bucket_name                = var.app_s3_bucket_name
  app_s3_versioning_enabled         = var.app_s3_versioning_enabled
  app_s3_enable_public_read         = var.app_s3_enable_public_read
  app_s3_enable_cloudfront          = var.app_s3_enable_cloudfront
  app_s3_cloudfront_price_class     = var.app_s3_cloudfront_price_class
  app_s3_enable_intelligent_tiering = var.app_s3_enable_intelligent_tiering

  # Database
  db_name                        = var.db_name
  db_username                    = var.db_username
  db_password                    = var.db_password
  db_manage_master_user_password = var.db_manage_master_user_password
  db_instance_class              = var.db_instance_class
  db_allocated_storage           = var.db_allocated_storage
  db_max_allocated_storage       = var.db_max_allocated_storage
  db_engine_version              = var.db_engine_version
  db_multi_az                    = var.db_multi_az
  db_backup_retention_period     = var.db_backup_retention_period
  db_deletion_protection         = var.db_deletion_protection
  db_enable_performance_insights = var.db_enable_performance_insights
  db_enable_enhanced_monitoring  = var.db_enable_enhanced_monitoring
  db_create_parameter_group      = var.db_create_parameter_group
  db_parameters                  = var.db_parameters

  # Redis
  redis_node_type                  = var.redis_node_type
  redis_num_cache_clusters         = var.redis_num_cache_clusters
  redis_automatic_failover_enabled = var.redis_automatic_failover_enabled

  # Container Images
  container_registry  = var.container_registry
  container_image_tag = var.container_image_tag
  api_image_name      = var.api_image_name

  # API Service
  api_container_port           = var.api_container_port
  api_cpu                      = var.api_cpu
  api_memory                   = var.api_memory
  api_desired_count            = var.api_desired_count
  api_enable_circuit_breaker   = var.api_enable_circuit_breaker
  api_use_fargate_spot         = var.api_use_fargate_spot
  api_enable_autoscaling       = var.api_enable_autoscaling
  api_autoscaling_min_capacity = var.api_autoscaling_min_capacity
  api_autoscaling_max_capacity = var.api_autoscaling_max_capacity
  api_autoscaling_cpu_target   = var.api_autoscaling_cpu_target

}
