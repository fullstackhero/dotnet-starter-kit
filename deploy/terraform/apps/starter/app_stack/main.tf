################################################################################
# Local Variables
################################################################################

locals {
  common_tags = merge(
    {
      Environment = var.environment
      Project     = "dotnet-starter-kit"
      ManagedBy   = "terraform"
    },
    var.owner != null ? { Owner = var.owner } : {}
  )
  aspnetcore_environment = var.environment == "dev" ? "Development" : "Production"
  name_prefix            = "${var.environment}-${var.region}"

  # Container images constructed from registry, name, and tag
  api_container_image    = "${var.container_registry}/${var.api_image_name}:${var.container_image_tag}"
}

################################################################################
# Network
################################################################################

module "network" {
  source = "../../../modules/network"

  name       = local.name_prefix
  cidr_block = var.vpc_cidr_block

  public_subnets  = var.public_subnets
  private_subnets = var.private_subnets

  enable_nat_gateway = var.enable_nat_gateway
  single_nat_gateway = var.single_nat_gateway

  enable_s3_endpoint             = var.enable_s3_endpoint
  enable_ecr_endpoints           = var.enable_ecr_endpoints
  enable_logs_endpoint           = var.enable_logs_endpoint
  enable_secretsmanager_endpoint = var.enable_secretsmanager_endpoint

  enable_flow_logs        = var.enable_flow_logs
  flow_logs_retention_days = var.flow_logs_retention_days

  tags = local.common_tags
}

################################################################################
# ECS Cluster
################################################################################

module "ecs_cluster" {
  source = "../../../modules/ecs_cluster"

  name               = "${local.name_prefix}-cluster"
  container_insights = var.enable_container_insights
  tags               = local.common_tags
}

################################################################################
# ALB Security Group
################################################################################

resource "aws_security_group" "alb" {
  name        = "${local.name_prefix}-alb"
  description = "ALB security group"
  vpc_id      = module.network.vpc_id

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-alb"
  })
}

resource "aws_vpc_security_group_ingress_rule" "alb_http" {
  security_group_id = aws_security_group.alb.id
  description       = "HTTP from anywhere"
  from_port         = 80
  to_port           = 80
  ip_protocol       = "tcp"
  cidr_ipv4         = "0.0.0.0/0"
}

resource "aws_vpc_security_group_ingress_rule" "alb_https" {
  count             = var.enable_https ? 1 : 0
  security_group_id = aws_security_group.alb.id
  description       = "HTTPS from anywhere"
  from_port         = 443
  to_port           = 443
  ip_protocol       = "tcp"
  cidr_ipv4         = "0.0.0.0/0"
}

resource "aws_vpc_security_group_egress_rule" "alb_all" {
  security_group_id = aws_security_group.alb.id
  description       = "All outbound traffic"
  ip_protocol       = "-1"
  cidr_ipv4         = "0.0.0.0/0"
}

################################################################################
# Application Load Balancer
################################################################################

module "alb" {
  source = "../../../modules/alb"

  name              = "${local.name_prefix}-alb"
  subnet_ids        = module.network.public_subnet_ids
  security_group_id = aws_security_group.alb.id

  enable_https    = var.enable_https
  certificate_arn = var.acm_certificate_arn
  ssl_policy      = var.ssl_policy

  enable_deletion_protection = var.alb_enable_deletion_protection
  idle_timeout               = var.alb_idle_timeout

  access_logs_bucket = var.alb_access_logs_bucket
  access_logs_prefix = var.alb_access_logs_prefix

  tags = local.common_tags
}

################################################################################
# WAF (Web Application Firewall)
################################################################################

module "waf" {
  count  = var.enable_waf ? 1 : 0
  source = "../../../modules/waf"

  name    = "${local.name_prefix}-waf"
  alb_arn = module.alb.arn

  rate_limit                    = var.waf_rate_limit
  enable_sqli_rule_set          = var.waf_enable_sqli_rule_set
  enable_ip_reputation_rule_set = var.waf_enable_ip_reputation_rule_set
  enable_anonymous_ip_rule_set  = var.waf_enable_anonymous_ip_rule_set
  enable_linux_rule_set         = var.waf_enable_linux_rule_set
  enable_logging                = var.waf_enable_logging

  tags = local.common_tags
}

################################################################################
# S3 Bucket for Application Data
################################################################################

module "app_s3" {
  source = "../../../modules/s3_bucket"

  name               = var.app_s3_bucket_name
  force_destroy      = var.environment == "dev" ? true : false
  versioning_enabled = var.app_s3_versioning_enabled

  enable_public_read = var.app_s3_enable_public_read
  public_read_prefix = var.app_s3_public_read_prefix

  enable_cloudfront          = var.app_s3_enable_cloudfront
  cloudfront_price_class     = var.app_s3_cloudfront_price_class
  cloudfront_aliases         = var.app_s3_cloudfront_aliases
  cloudfront_acm_certificate_arn = var.app_s3_cloudfront_certificate_arn

  enable_intelligent_tiering = var.app_s3_enable_intelligent_tiering
  lifecycle_rules            = var.app_s3_lifecycle_rules

  cors_rules = var.enable_https && var.domain_name != null ? [
    {
      allowed_methods = ["GET", "PUT", "POST"]
      allowed_origins = ["https://${var.domain_name}"]
      allowed_headers = ["*"]
      expose_headers  = ["ETag"]
      max_age_seconds = 3600
    }
  ] : []

  tags = local.common_tags
}

################################################################################
# IAM Role for API Task
################################################################################

data "aws_iam_policy_document" "api_task_assume" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

data "aws_iam_policy_document" "api_task_s3" {
  statement {
    sid = "AllowBucketReadWrite"
    actions = [
      "s3:PutObject",
      "s3:DeleteObject",
      "s3:GetObject",
      "s3:ListBucket"
    ]
    resources = [
      module.app_s3.bucket_arn,
      "${module.app_s3.bucket_arn}/*"
    ]
  }
}

# Note: The api_task role doesn't need secrets access because the connection string
# is injected via ECS secrets (handled by task execution role in ecs_service module).
# This policy document is kept for potential future runtime secret access needs.
data "aws_iam_policy_document" "api_task_secrets" {
  count = var.db_manage_master_user_password ? 1 : 0

  statement {
    sid = "AllowSecretsAccess"
    actions = [
      "secretsmanager:GetSecretValue"
    ]
    resources = [
      aws_secretsmanager_secret.db_connection_string[0].arn
    ]
  }
}

resource "aws_iam_role" "api_task" {
  name               = "${var.environment}-api-task"
  assume_role_policy = data.aws_iam_policy_document.api_task_assume.json
  tags               = local.common_tags
}

resource "aws_iam_role_policy" "api_task_s3" {
  name   = "${var.environment}-api-task-s3"
  role   = aws_iam_role.api_task.id
  policy = data.aws_iam_policy_document.api_task_s3.json
}

resource "aws_iam_role_policy" "api_task_secrets" {
  count  = var.db_manage_master_user_password ? 1 : 0
  name   = "${var.environment}-api-task-secrets"
  role   = aws_iam_role.api_task.id
  policy = data.aws_iam_policy_document.api_task_secrets[0].json
}

################################################################################
# RDS PostgreSQL
################################################################################

module "rds" {
  source = "../../../modules/rds_postgres"

  name           = "${local.name_prefix}-postgres"
  vpc_id         = module.network.vpc_id
  vpc_cidr_block = var.vpc_cidr_block
  subnet_ids     = module.network.private_subnet_ids

  # Use CIDR block to allow access from private subnets (ECS services)
  allowed_cidr_blocks = [var.vpc_cidr_block]

  db_name  = var.db_name
  username = var.db_username

  manage_master_user_password = var.db_manage_master_user_password
  password                    = var.db_manage_master_user_password ? null : var.db_password

  instance_class        = var.db_instance_class
  allocated_storage     = var.db_allocated_storage
  max_allocated_storage = var.db_max_allocated_storage
  storage_type          = var.db_storage_type
  engine_version        = var.db_engine_version

  multi_az                    = var.db_multi_az
  backup_retention_period     = var.db_backup_retention_period
  deletion_protection         = var.db_deletion_protection
  skip_final_snapshot         = var.environment == "dev" ? true : false
  final_snapshot_identifier   = var.environment != "dev" ? "${local.name_prefix}-postgres-final" : null

  performance_insights_enabled = var.db_enable_performance_insights
  monitoring_interval          = var.db_enable_enhanced_monitoring ? var.db_monitoring_interval : 0

  create_parameter_group = var.db_create_parameter_group
  parameters             = var.db_parameters

  tags = local.common_tags
}

################################################################################
# Connection String Secret (for managed password)
# When using AWS-managed password, we need to create a separate secret that
# contains the full connection string, constructed using the password from
# the RDS-managed secret.
################################################################################

# Read the RDS-managed secret to get the password
data "aws_secretsmanager_secret_version" "rds_password" {
  count     = var.db_manage_master_user_password ? 1 : 0
  secret_id = module.rds.secret_arn
}

# Create a secret for the full connection string
resource "aws_secretsmanager_secret" "db_connection_string" {
  count = var.db_manage_master_user_password ? 1 : 0

  name        = "${var.environment}-db-connection-string"
  description = "Full PostgreSQL connection string for .NET application"

  tags = local.common_tags
}

resource "aws_secretsmanager_secret_version" "db_connection_string" {
  count     = var.db_manage_master_user_password ? 1 : 0
  secret_id = aws_secretsmanager_secret.db_connection_string[0].id

  secret_string = "Host=${module.rds.endpoint};Port=${module.rds.port};Database=${var.db_name};Username=${var.db_username};Password=${jsondecode(data.aws_secretsmanager_secret_version.rds_password[0].secret_string)["password"]};Pooling=true;SSL Mode=Require;Trust Server Certificate=true;"
}

locals {
  # Connection string for non-managed password (directly in env var)
  # Only constructed when db_password is provided (i.e., not using managed password)
  db_connection_string_plain = var.db_manage_master_user_password ? "" : "Host=${module.rds.endpoint};Port=${module.rds.port};Database=${var.db_name};Username=${var.db_username};Password=${var.db_password};Pooling=true;SSL Mode=Require;Trust Server Certificate=true;"
}

################################################################################
# ElastiCache Redis
################################################################################

module "redis" {
  source = "../../../modules/elasticache_redis"

  name           = "${local.name_prefix}-redis"
  vpc_id         = module.network.vpc_id
  vpc_cidr_block = var.vpc_cidr_block
  subnet_ids     = module.network.private_subnet_ids

  # Use CIDR block to allow access from private subnets (ECS services)
  allowed_cidr_blocks = [var.vpc_cidr_block]

  node_type                  = var.redis_node_type
  num_cache_clusters         = var.redis_num_cache_clusters
  engine_version             = var.redis_engine_version
  automatic_failover_enabled = var.redis_automatic_failover_enabled
  transit_encryption_enabled = var.redis_transit_encryption_enabled

  tags = local.common_tags
}

################################################################################
# API ECS Service
################################################################################

module "api_service" {
  source = "../../../modules/ecs_service"

  name            = "${var.environment}-api"
  region          = var.region
  cluster_arn     = module.ecs_cluster.arn
  container_image = local.api_container_image
  container_port  = var.api_container_port
  cpu             = var.api_cpu
  memory          = var.api_memory
  desired_count   = var.api_desired_count

  vpc_id           = module.network.vpc_id
  vpc_cidr_block   = module.network.vpc_cidr_block
  subnet_ids       = module.network.private_subnet_ids
  assign_public_ip = false

  listener_arn           = var.enable_https ? module.alb.https_listener_arn : module.alb.http_listener_arn
  listener_rule_priority = 10
  path_patterns          = ["/api/*", "/scalar*", "/health*", "/swagger*", "/openapi*"]

  health_check_path              = "/health/live"
  health_check_healthy_threshold = var.api_health_check_healthy_threshold
  deregistration_delay           = var.api_deregistration_delay

  task_role_arn = aws_iam_role.api_task.arn

  enable_circuit_breaker = var.api_enable_circuit_breaker
  use_fargate_spot       = var.api_use_fargate_spot

  # Auto-scaling
  enable_autoscaling       = var.api_enable_autoscaling
  autoscaling_min_capacity = var.api_autoscaling_min_capacity
  autoscaling_max_capacity = var.api_autoscaling_max_capacity
  autoscaling_cpu_target   = var.api_autoscaling_cpu_target

  # When using managed password, connection string comes from secrets
  # When not using managed password, connection string is set directly in env vars
  environment_variables = merge(
    {
      ASPNETCORE_ENVIRONMENT = local.aspnetcore_environment
      CachingOptions__Redis  = module.redis.connection_string
      Storage__Provider      = "s3"
      Storage__S3__Bucket    = var.app_s3_bucket_name
      Storage__S3__PublicBaseUrl = module.app_s3.cloudfront_domain_name != "" ? "https://${module.app_s3.cloudfront_domain_name}" : ""
    },
    # Only set connection string in env vars when NOT using managed password
    var.db_manage_master_user_password ? {} : {
      DatabaseOptions__ConnectionString = local.db_connection_string_plain
    },
    var.enable_https && var.domain_name != null ? {
      OriginOptions__OriginUrl       = "https://${var.domain_name}"
      CorsOptions__AllowedOrigins__0 = "https://${var.domain_name}"
    } : {
      OriginOptions__OriginUrl       = "http://${module.alb.dns_name}"
      CorsOptions__AllowedOrigins__0 = "http://${module.alb.dns_name}"
    },
    var.api_extra_environment_variables
  )

  # When using managed password, inject the full connection string from Secrets Manager
  secrets = var.db_manage_master_user_password ? [
    {
      name      = "DatabaseOptions__ConnectionString"
      valueFrom = aws_secretsmanager_secret.db_connection_string[0].arn
    }
  ] : []

  tags = local.common_tags
}

################################################################################
# CloudWatch Alarms
################################################################################

module "alarms" {
  count  = var.enable_alarms ? 1 : 0
  source = "../../../modules/cloudwatch_alarms"

  name                 = local.name_prefix
  alarm_email_addresses = var.alarm_email_addresses

  ecs_services = {
    api = {
      cluster_name = module.ecs_cluster.name
      service_name = module.api_service.service_name
    }
  }

  rds_instance_identifier    = module.rds.identifier
  redis_replication_group_id = module.redis.replication_group_id
  alb_arn_suffix             = module.alb.arn_suffix

  alb_target_group_arns = {
    api = {
      target_group_arn_suffix = module.api_service.target_group_arn_suffix
    }
  }

  tags = local.common_tags
}

