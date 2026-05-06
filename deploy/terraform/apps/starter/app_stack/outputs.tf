################################################################################
# Network Outputs
################################################################################

output "vpc_id" {
  description = "VPC ID."
  value       = module.network.vpc_id
}

output "vpc_cidr_block" {
  description = "VPC CIDR block."
  value       = module.network.vpc_cidr_block
}

output "public_subnet_ids" {
  description = "Public subnet IDs."
  value       = module.network.public_subnet_ids
}

output "private_subnet_ids" {
  description = "Private subnet IDs."
  value       = module.network.private_subnet_ids
}

################################################################################
# ALB Outputs
################################################################################

output "alb_arn" {
  description = "ALB ARN."
  value       = module.alb.arn
}

output "alb_dns_name" {
  description = "ALB DNS name."
  value       = module.alb.dns_name
}

output "alb_zone_id" {
  description = "ALB hosted zone ID."
  value       = module.alb.zone_id
}

################################################################################
# Application URLs
################################################################################

output "api_url" {
  description = "API URL."
  value       = var.enable_https && var.domain_name != null ? "https://${var.domain_name}/api" : "http://${module.alb.dns_name}/api"
}

################################################################################
# ECS Outputs
################################################################################

output "ecs_cluster_id" {
  description = "ECS cluster ID."
  value       = module.ecs_cluster.id
}

output "ecs_cluster_arn" {
  description = "ECS cluster ARN."
  value       = module.ecs_cluster.arn
}

output "api_service_name" {
  description = "API ECS service name."
  value       = module.api_service.service_name
}

################################################################################
# Database Outputs
################################################################################

output "rds_endpoint" {
  description = "RDS endpoint."
  value       = module.rds.endpoint
}

output "rds_port" {
  description = "RDS port."
  value       = module.rds.port
}

output "rds_secret_arn" {
  description = "RDS Secrets Manager secret ARN (if manage_master_user_password is true)."
  value       = module.rds.secret_arn
}

################################################################################
# Redis Outputs
################################################################################

output "redis_endpoint" {
  description = "Redis primary endpoint address."
  value       = module.redis.primary_endpoint_address
}

output "redis_connection_string" {
  description = "Redis connection string for .NET applications."
  value       = module.redis.connection_string
  sensitive   = true
}

################################################################################
# S3 Outputs
################################################################################

output "s3_bucket_name" {
  description = "S3 bucket name."
  value       = module.app_s3.bucket_name
}

output "s3_bucket_arn" {
  description = "S3 bucket ARN."
  value       = module.app_s3.bucket_arn
}

output "s3_cloudfront_domain" {
  description = "CloudFront distribution domain name."
  value       = module.app_s3.cloudfront_domain_name
}

output "s3_cloudfront_distribution_id" {
  description = "CloudFront distribution ID."
  value       = module.app_s3.cloudfront_distribution_id
}

################################################################################
# WAF Outputs
################################################################################

output "waf_web_acl_arn" {
  description = "WAF Web ACL ARN (if WAF enabled)."
  value       = var.enable_waf ? module.waf[0].web_acl_arn : null
}

################################################################################
# Alarm Outputs
################################################################################

output "alarm_sns_topic_arn" {
  description = "SNS topic ARN for alarm notifications (if alarms enabled)."
  value       = var.enable_alarms ? module.alarms[0].sns_topic_arn : null
}
