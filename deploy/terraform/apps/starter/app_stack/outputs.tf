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
  value = (
    var.enable_https && var.domain_name != null ? "https://${var.domain_name}/api" :
    var.enable_api_cloudfront ? "https://${one(aws_cloudfront_distribution.api[*].domain_name)}/api" :
    "http://${module.alb.dns_name}/api"
  )
}

output "api_cloudfront_domain" {
  description = "CloudFront domain fronting the API (null unless enable_api_cloudfront)."
  value       = one(aws_cloudfront_distribution.api[*].domain_name)
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

output "migrator" {
  description = "DbMigrator one-shot task details for `aws ecs run-task` (deploy scripts), or null when not provisioned."
  value = var.enable_migrator ? {
    cluster_arn            = module.ecs_cluster.arn
    task_definition_family = module.migrator[0].task_definition_family
    container_name         = module.migrator[0].container_name
    security_group_id      = module.migrator[0].security_group_id
    subnet_ids             = module.network.private_subnet_ids
    log_group_name         = module.migrator[0].log_group_name
  } : null
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
# Application Secrets (generated)
################################################################################

output "hangfire_password" {
  description = "Generated Hangfire dashboard password (also in Secrets Manager)."
  value       = random_password.hangfire.result
  sensitive   = true
}

output "jwt_signing_key_secret_arn" {
  description = "Secrets Manager ARN holding the API's JWT signing key."
  value       = aws_secretsmanager_secret.jwt_signing_key.arn
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
# Frontend (React SPA) Outputs
################################################################################

output "dashboard_site" {
  description = "Dashboard SPA hosting details (bucket + CloudFront) for CI deploys, or null when not provisioned."
  value = var.enable_dashboard_site ? {
    bucket_name                = module.dashboard_site[0].bucket_name
    cloudfront_distribution_id = module.dashboard_site[0].cloudfront_distribution_id
    cloudfront_domain_name     = module.dashboard_site[0].cloudfront_domain_name
    cloudfront_hosted_zone_id  = module.dashboard_site[0].cloudfront_hosted_zone_id
    url                        = module.dashboard_site[0].url
  } : null
}

output "admin_site" {
  description = "Admin SPA hosting details (bucket + CloudFront) for CI deploys, or null when not provisioned."
  value = var.enable_admin_site ? {
    bucket_name                = module.admin_site[0].bucket_name
    cloudfront_distribution_id = module.admin_site[0].cloudfront_distribution_id
    cloudfront_domain_name     = module.admin_site[0].cloudfront_domain_name
    cloudfront_hosted_zone_id  = module.admin_site[0].cloudfront_hosted_zone_id
    url                        = module.admin_site[0].url
  } : null
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
