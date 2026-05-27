################################################################################
# DB Subnet Group
################################################################################

resource "aws_db_subnet_group" "this" {
  name       = "${var.name}-subnets"
  subnet_ids = var.subnet_ids

  tags = merge(var.tags, {
    Name = "${var.name}-subnet-group"
  })
}

################################################################################
# Security Group
################################################################################

resource "aws_security_group" "this" {
  name        = "${var.name}-sg"
  description = "Security group for RDS PostgreSQL ${var.name}"
  vpc_id      = var.vpc_id

  tags = merge(var.tags, {
    Name = "${var.name}-rds-sg"
  })
}

resource "aws_vpc_security_group_ingress_rule" "postgres_sg" {
  for_each = toset(var.allowed_security_group_ids)

  security_group_id            = aws_security_group.this.id
  description                  = "PostgreSQL access from allowed security group"
  from_port                    = 5432
  to_port                      = 5432
  ip_protocol                  = "tcp"
  referenced_security_group_id = each.value

  tags = var.tags
}

resource "aws_vpc_security_group_ingress_rule" "postgres_cidr" {
  count = length(var.allowed_cidr_blocks)

  security_group_id = aws_security_group.this.id
  description       = "PostgreSQL access from allowed CIDR block"
  from_port         = 5432
  to_port           = 5432
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
# Parameter Group (Optional)
################################################################################

resource "aws_db_parameter_group" "this" {
  count = var.create_parameter_group ? 1 : 0

  name   = "${var.name}-params"
  family = "postgres${split(".", var.engine_version)[0]}"

  dynamic "parameter" {
    for_each = var.parameters
    content {
      name         = parameter.value.name
      value        = parameter.value.value
      apply_method = parameter.value.apply_method
    }
  }

  tags = var.tags

  lifecycle {
    create_before_destroy = true
  }
}

################################################################################
# RDS Instance
################################################################################

resource "aws_db_instance" "this" {
  identifier = var.name

  # Engine
  engine         = "postgres"
  engine_version = var.engine_version

  # Instance
  instance_class        = var.instance_class
  allocated_storage     = var.allocated_storage
  max_allocated_storage = var.max_allocated_storage
  storage_type          = var.storage_type
  iops                  = var.storage_type == "io1" || var.storage_type == "io2" ? var.iops : null

  # Database
  db_name = var.db_name

  # Credentials - Support both managed and manual password
  username                    = var.username
  password                    = var.manage_master_user_password ? null : var.password
  manage_master_user_password = var.manage_master_user_password

  # Network
  db_subnet_group_name   = aws_db_subnet_group.this.name
  vpc_security_group_ids = [aws_security_group.this.id]
  publicly_accessible    = false
  port                   = 5432

  # Parameters
  parameter_group_name = var.create_parameter_group ? aws_db_parameter_group.this[0].name : null

  # Storage Encryption
  storage_encrypted = true
  kms_key_id        = var.kms_key_id

  # High Availability
  multi_az = var.multi_az

  # Backup
  backup_retention_period   = var.backup_retention_period
  backup_window             = var.backup_window
  maintenance_window        = var.maintenance_window
  copy_tags_to_snapshot     = true
  delete_automated_backups  = var.delete_automated_backups
  final_snapshot_identifier = var.skip_final_snapshot ? null : coalesce(var.final_snapshot_identifier, "${var.name}-final-snapshot")
  skip_final_snapshot       = var.skip_final_snapshot

  # Monitoring
  performance_insights_enabled          = var.performance_insights_enabled
  performance_insights_retention_period = var.performance_insights_enabled ? var.performance_insights_retention_period : null
  monitoring_interval                   = var.monitoring_interval
  monitoring_role_arn                   = var.monitoring_interval > 0 ? aws_iam_role.rds_monitoring[0].arn : null

  # Upgrades
  auto_minor_version_upgrade  = var.auto_minor_version_upgrade
  allow_major_version_upgrade = var.allow_major_version_upgrade
  apply_immediately           = var.apply_immediately

  # IAM Authentication
  iam_database_authentication_enabled = var.iam_database_authentication_enabled

  # CloudWatch Log Exports
  enabled_cloudwatch_logs_exports = var.cloudwatch_log_exports

  # Deletion Protection
  deletion_protection = var.deletion_protection

  tags = var.tags

  lifecycle {
    ignore_changes = [password]
  }
}

################################################################################
# Enhanced Monitoring IAM Role
################################################################################

resource "aws_iam_role" "rds_monitoring" {
  count = var.monitoring_interval > 0 ? 1 : 0

  name = "${var.name}-rds-monitoring"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "monitoring.rds.amazonaws.com"
      }
    }]
  })

  tags = var.tags
}

resource "aws_iam_role_policy_attachment" "rds_monitoring" {
  count = var.monitoring_interval > 0 ? 1 : 0

  role       = aws_iam_role.rds_monitoring[0].name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonRDSEnhancedMonitoringRole"
}
