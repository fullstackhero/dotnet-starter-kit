################################################################################
# Network
################################################################################

output "vpc_id" {
  description = "VPC ID."
  value       = module.app.vpc_id
}

output "alb_dns_name" {
  description = "ALB DNS name."
  value       = module.app.alb_dns_name
}

output "alb_zone_id" {
  description = "ALB hosted zone ID (for Route53 alias records)."
  value       = module.app.alb_zone_id
}

################################################################################
# Application URLs
################################################################################

output "api_url" {
  description = "API URL."
  value       = module.app.api_url
}

################################################################################
# Database
################################################################################

output "rds_endpoint" {
  description = "RDS endpoint."
  value       = module.app.rds_endpoint
}

output "rds_secret_arn" {
  description = "RDS secret ARN (if using managed password)."
  value       = module.app.rds_secret_arn
}

################################################################################
# Redis
################################################################################

output "redis_endpoint" {
  description = "Redis endpoint."
  value       = module.app.redis_endpoint
}

################################################################################
# S3
################################################################################

output "s3_bucket_name" {
  description = "S3 bucket name."
  value       = module.app.s3_bucket_name
}

output "s3_cloudfront_domain" {
  description = "CloudFront domain."
  value       = module.app.s3_cloudfront_domain != "" ? "https://${module.app.s3_cloudfront_domain}" : ""
}

output "s3_cloudfront_distribution_id" {
  description = "CloudFront distribution ID."
  value       = module.app.s3_cloudfront_distribution_id
}
