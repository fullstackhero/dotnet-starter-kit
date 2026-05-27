################################################################################
# ECS Cluster
################################################################################

resource "aws_ecs_cluster" "this" {
  name = var.name

  setting {
    name  = "containerInsights"
    value = var.container_insights ? "enabled" : "disabled"
  }

  configuration {
    execute_command_configuration {
      logging = var.enable_execute_command_logging ? "OVERRIDE" : "DEFAULT"

      dynamic "log_configuration" {
        for_each = var.enable_execute_command_logging ? [1] : []
        content {
          cloud_watch_log_group_name = aws_cloudwatch_log_group.execute_command[0].name
        }
      }
    }
  }

  tags = var.tags
}

################################################################################
# Cluster Capacity Providers
################################################################################

resource "aws_ecs_cluster_capacity_providers" "this" {
  cluster_name = aws_ecs_cluster.this.name

  capacity_providers = var.capacity_providers

  dynamic "default_capacity_provider_strategy" {
    for_each = var.default_capacity_provider_strategy
    content {
      capacity_provider = default_capacity_provider_strategy.value.capacity_provider
      weight            = default_capacity_provider_strategy.value.weight
      base              = default_capacity_provider_strategy.value.base
    }
  }
}

################################################################################
# Execute Command Logging
################################################################################

resource "aws_cloudwatch_log_group" "execute_command" {
  count = var.enable_execute_command_logging ? 1 : 0

  name              = "/aws/ecs/${var.name}/execute-command"
  retention_in_days = var.log_retention_in_days
  kms_key_id        = var.kms_key_id

  tags = var.tags
}
