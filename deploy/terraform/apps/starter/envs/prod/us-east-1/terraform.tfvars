################################################################################
# Production Environment — US East 1
################################################################################

environment = "prod"
region      = "us-east-1"

# Configure with your production domain
# domain_name         = "app.example.com"
# enable_https        = true
# acm_certificate_arn = "arn:aws:acm:us-east-1:ACCOUNT_ID:certificate/CERT_ID"

################################################################################
# Network (3 AZs, NAT per AZ for HA)
################################################################################

vpc_cidr_block = "10.30.0.0/16"

public_subnets = {
  a = { cidr_block = "10.30.0.0/24", az = "us-east-1a" }
  b = { cidr_block = "10.30.1.0/24", az = "us-east-1b" }
  c = { cidr_block = "10.30.2.0/24", az = "us-east-1c" }
}

private_subnets = {
  a = { cidr_block = "10.30.10.0/24", az = "us-east-1a" }
  b = { cidr_block = "10.30.11.0/24", az = "us-east-1b" }
  c = { cidr_block = "10.30.12.0/24", az = "us-east-1c" }
}

single_nat_gateway             = false
enable_s3_endpoint             = true
enable_ecr_endpoints           = true
enable_logs_endpoint           = true
enable_secretsmanager_endpoint = true
enable_flow_logs               = true
flow_logs_retention_days       = 90

################################################################################
# WAF
################################################################################

enable_waf                        = true
waf_rate_limit                    = 2000
waf_enable_sqli_rule_set          = true
waf_enable_ip_reputation_rule_set = true
waf_enable_linux_rule_set         = true
waf_enable_logging                = true

################################################################################
# S3
################################################################################

app_s3_bucket_name                = "prod-fsh-app-bucket"
app_s3_versioning_enabled         = true
app_s3_enable_public_read         = false
app_s3_enable_cloudfront          = true
app_s3_cloudfront_price_class     = "PriceClass_200"
app_s3_enable_intelligent_tiering = true

################################################################################
# Database
################################################################################

db_name                        = "fshdb"
db_username                    = "fshadmin"
db_manage_master_user_password = true
db_instance_class              = "db.t4g.medium"
db_allocated_storage           = 50
db_max_allocated_storage       = 200
db_multi_az                    = true
db_backup_retention_period     = 30
db_deletion_protection         = true
db_enable_performance_insights = true
db_enable_enhanced_monitoring  = true

# Production-tuned PostgreSQL parameters
db_create_parameter_group = true
db_parameters = [
  { name = "log_min_duration_statement", value = "1000", apply_method = "immediate" },
  { name = "shared_preload_libraries", value = "pg_stat_statements", apply_method = "pending-reboot" },
  { name = "pg_stat_statements.track", value = "all", apply_method = "immediate" },
  { name = "log_connections", value = "1", apply_method = "immediate" },
  { name = "log_disconnections", value = "1", apply_method = "immediate" }
]

################################################################################
# Redis
################################################################################

redis_node_type                  = "cache.t4g.medium"
redis_num_cache_clusters         = 2
redis_automatic_failover_enabled = true

################################################################################
# Container Images
################################################################################

# IMPORTANT: Always pin to a specific git SHA or semver tag.
container_image_tag = "v1.0.0"

################################################################################
# Services (no Spot for production stability)
################################################################################

api_cpu              = "1024"
api_memory           = "2048"
api_desired_count    = 3
api_use_fargate_spot = false

api_enable_autoscaling       = true
api_autoscaling_min_capacity = 3
api_autoscaling_max_capacity = 20

enable_container_insights = true

################################################################################
# ALB
################################################################################

alb_enable_deletion_protection = true

################################################################################
# Alarms
################################################################################

enable_alarms = true
# alarm_email_addresses = ["ops@example.com"]
