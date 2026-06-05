################################################################################
# Security Group
################################################################################

resource "aws_security_group" "this" {
  name        = "${var.name}-sg"
  description = "Security group for ElastiCache Redis ${var.name}"
  vpc_id      = var.vpc_id

  tags = merge(var.tags, {
    Name = "${var.name}-redis-sg"
  })
}

resource "aws_vpc_security_group_ingress_rule" "redis_sg" {
  for_each = toset(var.allowed_security_group_ids)

  security_group_id            = aws_security_group.this.id
  description                  = "Redis access from allowed security group"
  from_port                    = 6379
  to_port                      = 6379
  ip_protocol                  = "tcp"
  referenced_security_group_id = each.value

  tags = var.tags
}

resource "aws_vpc_security_group_ingress_rule" "redis_cidr" {
  count = length(var.allowed_cidr_blocks)

  security_group_id = aws_security_group.this.id
  description       = "Redis access from allowed CIDR block"
  from_port         = 6379
  to_port           = 6379
  ip_protocol       = "tcp"
  cidr_ipv4         = var.allowed_cidr_blocks[count.index]

  tags = var.tags
}

resource "aws_vpc_security_group_egress_rule" "vpc" {
  security_group_id = aws_security_group.this.id
  description       = "Allow outbound traffic within VPC only"
  ip_protocol       = "-1"
  cidr_ipv4         = var.vpc_cidr_block

  tags = var.tags
}

################################################################################
# Subnet Group
################################################################################

resource "aws_elasticache_subnet_group" "this" {
  name       = "${var.name}-subnets"
  subnet_ids = var.subnet_ids

  tags = merge(var.tags, {
    Name = "${var.name}-subnet-group"
  })
}

################################################################################
# Parameter Group (Optional)
################################################################################

resource "aws_elasticache_parameter_group" "this" {
  count = var.create_parameter_group ? 1 : 0

  name = "${var.name}-params"
  # Parameter-group family is engine-specific: valkey8 / valkey7 / redis7.
  family = "${var.engine}${split(".", var.engine_version)[0]}"

  dynamic "parameter" {
    for_each = var.parameters
    content {
      name  = parameter.value.name
      value = parameter.value.value
    }
  }

  tags = var.tags

  lifecycle {
    create_before_destroy = true
  }
}

################################################################################
# Replication Group
################################################################################

resource "aws_elasticache_replication_group" "this" {
  replication_group_id = var.name
  description          = var.description != "" ? var.description : "Redis for ${var.name}"

  # Engine
  engine         = var.engine
  engine_version = var.engine_version
  node_type      = var.node_type

  # Cluster Configuration
  num_cache_clusters         = var.num_cache_clusters
  automatic_failover_enabled = var.automatic_failover_enabled
  multi_az_enabled           = var.multi_az_enabled

  # Network
  port                 = 6379
  subnet_group_name    = aws_elasticache_subnet_group.this.name
  security_group_ids   = [aws_security_group.this.id]
  parameter_group_name = var.create_parameter_group ? aws_elasticache_parameter_group.this[0].name : null

  # Security
  at_rest_encryption_enabled = true
  transit_encryption_enabled = var.transit_encryption_enabled
  auth_token                 = var.auth_token
  kms_key_id                 = var.kms_key_id

  # Maintenance
  auto_minor_version_upgrade = var.auto_minor_version_upgrade
  apply_immediately          = var.apply_immediately
  maintenance_window         = var.maintenance_window

  # Snapshots
  snapshot_retention_limit  = var.snapshot_retention_limit
  snapshot_window           = var.snapshot_window
  final_snapshot_identifier = var.skip_final_snapshot ? null : "${var.name}-final-snapshot"

  # Notifications
  notification_topic_arn = var.notification_topic_arn

  # Log Delivery
  dynamic "log_delivery_configuration" {
    for_each = var.enable_slow_log ? [1] : []
    content {
      destination      = aws_cloudwatch_log_group.slow_log[0].name
      destination_type = "cloudwatch-logs"
      log_format       = "json"
      log_type         = "slow-log"
    }
  }

  dynamic "log_delivery_configuration" {
    for_each = var.enable_engine_log ? [1] : []
    content {
      destination      = aws_cloudwatch_log_group.engine_log[0].name
      destination_type = "cloudwatch-logs"
      log_format       = "json"
      log_type         = "engine-log"
    }
  }

  tags = var.tags

  lifecycle {
    ignore_changes = [auth_token]
  }
}

################################################################################
# Log Groups
################################################################################

resource "aws_cloudwatch_log_group" "slow_log" {
  count = var.enable_slow_log ? 1 : 0

  name              = "/aws/elasticache/${var.name}/slow-log"
  retention_in_days = var.log_retention_in_days
  kms_key_id        = var.kms_key_id

  tags = var.tags
}

resource "aws_cloudwatch_log_group" "engine_log" {
  count = var.enable_engine_log ? 1 : 0

  name              = "/aws/elasticache/${var.name}/engine-log"
  retention_in_days = var.log_retention_in_days
  kms_key_id        = var.kms_key_id

  tags = var.tags
}
