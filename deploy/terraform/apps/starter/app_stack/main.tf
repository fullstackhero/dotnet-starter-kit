################################################################################
# Local Variables
################################################################################

locals {
  # Environment/Project/ManagedBy/Owner are applied globally via provider
  # default_tags (see providers.tf); modules only add their own Name tag, so
  # this map is intentionally empty unless extra per-stack tags are needed.
  common_tags = {}

  aspnetcore_environment = var.environment == "dev" ? "Development" : "Production"
  name_prefix            = "${var.environment}-${var.region}"

  # Container image constructed from registry, name, and tag.
  api_container_image = "${var.container_registry}/${var.api_image_name}:${var.container_image_tag}"

  # DbMigrator image — same registry + tag as the API, different repository.
  # Run as a one-shot ECS task (apply / apply --seed) by the deploy scripts.
  migrator_container_image = "${var.container_registry}/${var.migrator_image_name}:${var.container_image_tag}"

  # Public origin of the API (no /api suffix) — used for the app OriginUrl and
  # as the apiBase the React SPAs read from their runtime config.json. HTTPS via
  # a custom domain when set, else via the API CloudFront distribution (free
  # *.cloudfront.net cert), else plain HTTP on the ALB.
  api_origin = (
    var.enable_https && var.domain_name != null ? "https://${var.domain_name}" :
    var.enable_api_cloudfront ? "https://${one(aws_cloudfront_distribution.api[*].domain_name)}" :
    "http://${module.alb.dns_name}"
  )
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

  enable_flow_logs         = var.enable_flow_logs
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

  enable_cloudfront              = var.app_s3_enable_cloudfront
  cloudfront_price_class         = var.app_s3_cloudfront_price_class
  cloudfront_aliases             = var.app_s3_cloudfront_aliases
  cloudfront_acm_certificate_arn = var.app_s3_cloudfront_certificate_arn

  enable_intelligent_tiering = var.app_s3_enable_intelligent_tiering
  lifecycle_rules            = var.app_s3_lifecycle_rules

  # Browser presigned uploads (My Files, chat attachments) PUT directly from the
  # SPA origins to S3, so the bucket must allow those origins — not just a custom
  # domain. Reuse the API's allow-list (SPA CloudFront/alias origins + custom
  # domain + extras), so it works on the default CloudFront domains too.
  cors_rules = local.has_cors_origins ? [
    {
      allowed_methods = ["GET", "PUT", "POST"]
      allowed_origins = local.cors_allowed_origins
      allowed_headers = ["*"]
      expose_headers  = ["ETag"]
      max_age_seconds = 3600
    }
  ] : []

  tags = local.common_tags
}

################################################################################
# React SPA hosting (S3 private origin + CloudFront)
#
# Two single-page apps replace the old server-rendered Blazor service:
#   - dashboard (clients/dashboard) — tenant-facing
#   - admin     (clients/admin)     — operator-facing
# Each gets its own private bucket + CloudFront distribution. Terraform owns
# config.json so the API/dashboard URLs are wired without rebuilding the SPA.
# CI publishes builds with:
#   aws s3 sync clients/<app>/dist s3://<bucket> --delete --exclude config.json
#   aws cloudfront create-invalidation --distribution-id <id> --paths '/*'
################################################################################

module "dashboard_site" {
  count  = var.enable_dashboard_site ? 1 : 0
  source = "../../../modules/static_site"

  name          = var.dashboard_s3_bucket_name
  force_destroy = var.environment == "dev"
  comment       = "${local.name_prefix} dashboard (tenant) SPA"

  price_class                = var.frontend_cloudfront_price_class
  aliases                    = var.dashboard_cloudfront_aliases
  acm_certificate_arn        = var.dashboard_cloudfront_certificate_arn
  response_headers_policy_id = var.frontend_response_headers_policy_id

  runtime_config = {
    apiBase       = local.api_origin
    defaultTenant = var.frontend_default_tenant
    demoMode      = var.dashboard_demo_mode
  }

  tags = local.common_tags
}

module "admin_site" {
  count  = var.enable_admin_site ? 1 : 0
  source = "../../../modules/static_site"

  name          = var.admin_s3_bucket_name
  force_destroy = var.environment == "dev"
  comment       = "${local.name_prefix} admin (operator) SPA"

  price_class                = var.frontend_cloudfront_price_class
  aliases                    = var.admin_cloudfront_aliases
  acm_certificate_arn        = var.admin_cloudfront_certificate_arn
  response_headers_policy_id = var.frontend_response_headers_policy_id

  runtime_config = {
    apiBase       = local.api_origin
    defaultTenant = var.frontend_default_tenant
    # The admin app links to the tenant dashboard for the impersonation handoff.
    dashboardUrl = local.dashboard_url
  }

  tags = local.common_tags
}

################################################################################
# API CloudFront — HTTPS in front of the (HTTP-only) ALB without a custom
# domain. Viewer↔CloudFront is HTTPS on the free *.cloudfront.net cert; the
# CloudFront↔ALB hop stays HTTP inside AWS. Dynamic API: no caching, forward
# everything (incl. Authorization) via the managed AllViewerExceptHostHeader.
################################################################################

data "aws_cloudfront_cache_policy" "caching_disabled" {
  count = var.enable_api_cloudfront ? 1 : 0
  name  = "Managed-CachingDisabled"
}

data "aws_cloudfront_origin_request_policy" "all_viewer_except_host" {
  count = var.enable_api_cloudfront ? 1 : 0
  name  = "Managed-AllViewerExceptHostHeader"
}

resource "aws_cloudfront_distribution" "api" {
  count       = var.enable_api_cloudfront ? 1 : 0
  enabled     = true
  comment     = "${local.name_prefix} API (ALB origin)"
  price_class = var.frontend_cloudfront_price_class

  origin {
    domain_name = module.alb.dns_name
    origin_id   = "alb"

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  default_cache_behavior {
    target_origin_id         = "alb"
    viewer_protocol_policy   = "redirect-to-https"
    allowed_methods          = ["GET", "HEAD", "OPTIONS", "PUT", "POST", "PATCH", "DELETE"]
    cached_methods           = ["GET", "HEAD"]
    cache_policy_id          = data.aws_cloudfront_cache_policy.caching_disabled[0].id
    origin_request_policy_id = data.aws_cloudfront_origin_request_policy.all_viewer_except_host[0].id
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }

  tags = local.common_tags
}

locals {
  # Resolved SPA origins (custom alias when set, else the CloudFront domain).
  # Fall back to an override for SPAs hosted outside this stack.
  admin_url     = var.enable_admin_site ? module.admin_site[0].url : (var.admin_url != null ? var.admin_url : "")
  dashboard_url = var.enable_dashboard_site ? module.dashboard_site[0].url : (var.dashboard_url != null ? var.dashboard_url : "")

  # Origins the API must accept browser requests from. The SPAs live on
  # CloudFront (cross-origin from the API), so they must be allow-listed.
  cors_allowed_origins = compact(concat(
    [local.admin_url, local.dashboard_url],
    var.enable_https && var.domain_name != null ? ["https://${var.domain_name}"] : [],
    var.api_extra_cors_origins,
  ))

  # Whether to emit the app bucket's CORS rule. Must be a PLAN-KNOWN boolean: the
  # resolved origins above are CloudFront domains that don't exist until apply, so
  # length(cors_allowed_origins) is unknown on first apply — feeding that into the
  # s3_bucket module's `count` fails with "Invalid count argument". Gate on the
  # known inputs that produce those origins instead.
  has_cors_origins = (
    var.enable_admin_site
    || var.enable_dashboard_site
    || var.admin_url != null
    || var.dashboard_url != null
    || (var.enable_https && var.domain_name != null)
    || length(var.api_extra_cors_origins) > 0
  )

  cors_environment_variables = {
    for idx, origin in local.cors_allowed_origins :
    "CorsOptions__AllowedOrigins__${idx}" => origin
  }
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

resource "aws_iam_role" "api_task" {
  name               = "${var.environment}-api-task"
  assume_role_policy = data.aws_iam_policy_document.api_task_assume.json
  tags               = local.common_tags
}

# The task role only needs S3 (application file storage). The DB connection
# string is injected as an env var from Secrets Manager by the task EXECUTION
# role (see the ecs_service module), so the task role needs no secrets access.
resource "aws_iam_role_policy" "api_task_s3" {
  name   = "${var.environment}-api-task-s3"
  role   = aws_iam_role.api_task.id
  policy = data.aws_iam_policy_document.api_task_s3.json
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

  multi_az                  = var.db_multi_az
  backup_retention_period   = var.db_backup_retention_period
  deletion_protection       = var.db_deletion_protection
  skip_final_snapshot       = var.environment == "dev" ? true : false
  final_snapshot_identifier = var.environment != "dev" ? "${local.name_prefix}-postgres-final" : null

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
# Application Auth Secrets (generated → Secrets Manager → injected into tasks)
#
# The API hard-requires JwtOptions:SigningKey and HangfireOptions:Username/
# Password (Require() in Production + ValidateOnStart in every env). Rather than
# lean on the public dev appsettings (forgeable JWTs), generate them here and
# inject via the task execution role like the DB connection string.
################################################################################

resource "random_password" "jwt_signing_key" {
  length  = 64
  special = false # consumed as ASCII bytes for HMAC — keep it alphanumeric
}

resource "random_password" "hangfire" {
  length           = 24
  special          = true
  override_special = "!@#%^-_=+"
}

resource "aws_secretsmanager_secret" "jwt_signing_key" {
  name        = "${var.environment}-jwt-signing-key"
  description = "HMAC signing key for FSH API JWTs."
  tags        = local.common_tags
}

resource "aws_secretsmanager_secret_version" "jwt_signing_key" {
  secret_id     = aws_secretsmanager_secret.jwt_signing_key.id
  secret_string = random_password.jwt_signing_key.result
}

resource "aws_secretsmanager_secret" "hangfire_password" {
  name        = "${var.environment}-hangfire-password"
  description = "Hangfire dashboard password for the FSH API."
  tags        = local.common_tags
}

resource "aws_secretsmanager_secret_version" "hangfire_password" {
  secret_id     = aws_secretsmanager_secret.hangfire_password.id
  secret_string = random_password.hangfire.result
}

locals {
  # Auth secrets injected into BOTH the API and the migrator task (the migrator
  # otherwise self-injects a placeholder signing key; a real one is harmless).
  app_auth_secrets = [
    {
      name      = "JwtOptions__SigningKey"
      valueFrom = aws_secretsmanager_secret.jwt_signing_key.arn
    },
    {
      name      = "HangfireOptions__Password"
      valueFrom = aws_secretsmanager_secret.hangfire_password.arn
    },
  ]

  # Optional dev seed credentials — injected only when set (prod runs `apply`
  # without --seed, so it never reads them).
  seed_environment_variables = merge(
    var.seed_default_admin_password != null ? { "Seed__DefaultAdminPassword" = var.seed_default_admin_password } : {},
    var.seed_demo_password != null ? { "Seed__DemoPassword" = var.seed_demo_password } : {},
  )
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
  engine                     = var.redis_engine
  engine_version             = var.redis_engine_version
  automatic_failover_enabled = var.redis_automatic_failover_enabled
  transit_encryption_enabled = var.redis_transit_encryption_enabled

  # dev tears down without a final snapshot (matches RDS above); otherwise a
  # repeated destroy collides with the prior attempt's snapshot name
  # (SnapshotAlreadyExistsFault). staging/prod keep the final snapshot.
  skip_final_snapshot = var.environment == "dev" ? true : false

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
      ASPNETCORE_ENVIRONMENT     = local.aspnetcore_environment
      CachingOptions__Redis      = module.redis.connection_string
      Storage__Provider          = "s3"
      Storage__S3__Bucket        = var.app_s3_bucket_name
      Storage__S3__PublicBaseUrl = module.app_s3.cloudfront_domain_name != "" ? "https://${module.app_s3.cloudfront_domain_name}" : ""
      OriginOptions__OriginUrl   = local.api_origin
      # Hangfire dashboard user (password arrives via `secrets` below). Both are
      # [Required] + ValidateOnStart, so the API won't boot without them.
      HangfireOptions__Username = var.hangfire_username
    },
    # Connection string is injected via env var only when NOT using a managed
    # (Secrets Manager) password; otherwise it arrives via `secrets` below.
    var.db_manage_master_user_password ? {} : {
      DatabaseOptions__ConnectionString = local.db_connection_string_plain
    },
    # CorsOptions__AllowedOrigins__0..N — the React SPA origins plus the app
    # domain, so browsers on those origins can call the API cross-origin.
    local.cors_environment_variables,
    var.api_extra_environment_variables
  )

  # Injected from Secrets Manager by the task execution role: the JWT signing
  # key + Hangfire password always, plus the DB connection string when managed.
  secrets = concat(
    local.app_auth_secrets,
    var.db_manage_master_user_password ? [
      {
        name      = "DatabaseOptions__ConnectionString"
        valueFrom = aws_secretsmanager_secret.db_connection_string[0].arn
      }
    ] : []
  )

  tags = local.common_tags
}

################################################################################
# DbMigrator — one-shot ECS task
#
# Registers a runnable task definition only (no service). The deploy scripts
# invoke it with `aws ecs run-task` after `apply` and wait for exit code 0.
# Command is environment-driven: dev runs `apply --seed`, prod runs `apply`
# (set per-env in terraform.tfvars via `migrator_command`). Seed credentials come
# from seed_default_admin_password / seed_demo_password when set (else the baked
# dev appsettings); the JWT signing key is injected from Secrets Manager.
################################################################################

module "migrator" {
  count  = var.enable_migrator ? 1 : 0
  source = "../../../modules/ecs_task"

  name            = "${var.environment}-db-migrator"
  region          = var.region
  vpc_id          = module.network.vpc_id
  container_image = local.migrator_container_image
  command         = var.migrator_command
  cpu             = var.migrator_cpu
  memory          = var.migrator_memory

  environment_variables = merge(
    {
      # Generic-host migrator selects its env from DOTNET_ENVIRONMENT, not ASPNETCORE_ENVIRONMENT.
      DOTNET_ENVIRONMENT                  = local.aspnetcore_environment
      DatabaseOptions__Provider           = "POSTGRESQL"
      DatabaseOptions__MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL"
      HangfireOptions__Username           = var.hangfire_username
    },
    # Plain connection string only when NOT using a managed (Secrets Manager)
    # password; otherwise it arrives via `secrets` below — same as the API.
    var.db_manage_master_user_password ? {} : {
      DatabaseOptions__ConnectionString = local.db_connection_string_plain
    },
    # Seed credentials (when configured) so seeding doesn't depend on the baked
    # dev appsettings; empty when unset → falls back to image config.
    local.seed_environment_variables,
    var.migrator_extra_environment_variables
  )

  secrets = concat(
    local.app_auth_secrets,
    var.db_manage_master_user_password ? [
      {
        name      = "DatabaseOptions__ConnectionString"
        valueFrom = aws_secretsmanager_secret.db_connection_string[0].arn
      }
    ] : []
  )

  tags = local.common_tags
}

################################################################################
# CloudWatch Alarms
################################################################################

module "alarms" {
  count  = var.enable_alarms ? 1 : 0
  source = "../../../modules/cloudwatch_alarms"

  name                  = local.name_prefix
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

