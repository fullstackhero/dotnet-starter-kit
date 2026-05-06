################################################################################
# Staging Environment — US East 1
################################################################################

environment = "staging"
region      = "us-east-1"

# domain_name         = "staging.example.com"
# enable_https        = true
# acm_certificate_arn = "arn:aws:acm:us-east-1:ACCOUNT_ID:certificate/CERT_ID"

################################################################################
# Network
################################################################################

vpc_cidr_block = "10.20.0.0/16"

public_subnets = {
  a = { cidr_block = "10.20.0.0/24", az = "us-east-1a" }
  b = { cidr_block = "10.20.1.0/24", az = "us-east-1b" }
}

private_subnets = {
  a = { cidr_block = "10.20.10.0/24", az = "us-east-1a" }
  b = { cidr_block = "10.20.11.0/24", az = "us-east-1b" }
}

single_nat_gateway             = true
enable_s3_endpoint             = true
enable_ecr_endpoints           = true
enable_logs_endpoint           = true
enable_secretsmanager_endpoint = true
enable_flow_logs               = true
flow_logs_retention_days       = 30

################################################################################
# WAF
################################################################################

enable_waf                        = true
waf_rate_limit                    = 2000
waf_enable_sqli_rule_set          = true
waf_enable_ip_reputation_rule_set = true
waf_enable_logging                = true

################################################################################
# S3
################################################################################

app_s3_bucket_name        = "staging-fsh-app-bucket"
app_s3_enable_public_read = false
app_s3_enable_cloudfront  = true

################################################################################
# Database
################################################################################

db_name                        = "fshdb"
db_username                    = "fshadmin"
db_manage_master_user_password = true
db_instance_class              = "db.t4g.small"
db_enable_performance_insights = true
db_deletion_protection         = true

################################################################################
# Redis
################################################################################

redis_node_type = "cache.t4g.small"

################################################################################
# Container Images
################################################################################

container_image_tag = "v0.1.0-rc1"

################################################################################
# Services
################################################################################

api_desired_count    = 2
api_use_fargate_spot = true

api_enable_autoscaling       = true
api_autoscaling_min_capacity = 2
api_autoscaling_max_capacity = 6

enable_container_insights = true

################################################################################
# Alarms
################################################################################

enable_alarms = true
# alarm_email_addresses = ["ops@example.com"]
