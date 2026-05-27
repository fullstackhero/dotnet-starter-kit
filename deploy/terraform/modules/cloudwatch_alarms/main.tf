################################################################################
# SNS Topic for Alarm Notifications
################################################################################

resource "aws_sns_topic" "alarms" {
  name              = "${var.name}-alarms"
  kms_master_key_id = var.kms_key_id

  tags = var.tags
}

resource "aws_sns_topic_subscription" "email" {
  for_each = toset(var.alarm_email_addresses)

  topic_arn = aws_sns_topic.alarms.arn
  protocol  = "email"
  endpoint  = each.value
}

################################################################################
# ECS Service Alarms
################################################################################

resource "aws_cloudwatch_metric_alarm" "ecs_cpu_high" {
  for_each = var.ecs_services

  alarm_name          = "${each.key}-cpu-high"
  alarm_description   = "ECS service ${each.key} CPU utilization above ${var.ecs_cpu_threshold}%"
  namespace           = "AWS/ECS"
  metric_name         = "CPUUtilization"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = var.ecs_cpu_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "breaching"

  dimensions = {
    ClusterName = each.value.cluster_name
    ServiceName = each.value.service_name
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "ecs_memory_high" {
  for_each = var.ecs_services

  alarm_name          = "${each.key}-memory-high"
  alarm_description   = "ECS service ${each.key} memory utilization above ${var.ecs_memory_threshold}%"
  namespace           = "AWS/ECS"
  metric_name         = "MemoryUtilization"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = var.ecs_memory_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "breaching"

  dimensions = {
    ClusterName = each.value.cluster_name
    ServiceName = each.value.service_name
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "ecs_running_tasks" {
  for_each = var.ecs_services

  alarm_name          = "${each.key}-no-running-tasks"
  alarm_description   = "ECS service ${each.key} has zero running tasks"
  namespace           = "AWS/ECS"
  metric_name         = "RunningTaskCount"
  statistic           = "Average"
  period              = 60
  evaluation_periods  = 2
  threshold           = 1
  comparison_operator = "LessThanThreshold"
  treat_missing_data  = "breaching"

  dimensions = {
    ClusterName = each.value.cluster_name
    ServiceName = each.value.service_name
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

################################################################################
# RDS Alarms
################################################################################

resource "aws_cloudwatch_metric_alarm" "rds_cpu_high" {
  count = var.rds_instance_identifier != null ? 1 : 0

  alarm_name          = "${var.name}-rds-cpu-high"
  alarm_description   = "RDS CPU utilization above ${var.rds_cpu_threshold}%"
  namespace           = "AWS/RDS"
  metric_name         = "CPUUtilization"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = var.rds_cpu_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "breaching"

  dimensions = {
    DBInstanceIdentifier = var.rds_instance_identifier
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "rds_free_storage_low" {
  count = var.rds_instance_identifier != null ? 1 : 0

  alarm_name          = "${var.name}-rds-storage-low"
  alarm_description   = "RDS free storage below ${var.rds_free_storage_threshold_gb} GB"
  namespace           = "AWS/RDS"
  metric_name         = "FreeStorageSpace"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 2
  threshold           = var.rds_free_storage_threshold_gb * 1073741824 # Convert GB to bytes
  comparison_operator = "LessThanThreshold"
  treat_missing_data  = "breaching"

  dimensions = {
    DBInstanceIdentifier = var.rds_instance_identifier
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "rds_connections_high" {
  count = var.rds_instance_identifier != null ? 1 : 0

  alarm_name          = "${var.name}-rds-connections-high"
  alarm_description   = "RDS connections above ${var.rds_connections_threshold}"
  namespace           = "AWS/RDS"
  metric_name         = "DatabaseConnections"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = var.rds_connections_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"

  dimensions = {
    DBInstanceIdentifier = var.rds_instance_identifier
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "rds_read_latency" {
  count = var.rds_instance_identifier != null ? 1 : 0

  alarm_name          = "${var.name}-rds-read-latency-high"
  alarm_description   = "RDS read latency above ${var.rds_read_latency_threshold_ms} ms"
  namespace           = "AWS/RDS"
  metric_name         = "ReadLatency"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = var.rds_read_latency_threshold_ms / 1000 # Convert ms to seconds
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"

  dimensions = {
    DBInstanceIdentifier = var.rds_instance_identifier
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

################################################################################
# ElastiCache Redis Alarms
################################################################################

resource "aws_cloudwatch_metric_alarm" "redis_cpu_high" {
  count = var.redis_replication_group_id != null ? 1 : 0

  alarm_name          = "${var.name}-redis-cpu-high"
  alarm_description   = "Redis engine CPU utilization above ${var.redis_cpu_threshold}%"
  namespace           = "AWS/ElastiCache"
  metric_name         = "EngineCPUUtilization"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = var.redis_cpu_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "breaching"

  dimensions = {
    ReplicationGroupId = var.redis_replication_group_id
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "redis_memory_high" {
  count = var.redis_replication_group_id != null ? 1 : 0

  alarm_name          = "${var.name}-redis-memory-high"
  alarm_description   = "Redis memory usage above ${var.redis_memory_threshold}%"
  namespace           = "AWS/ElastiCache"
  metric_name         = "DatabaseMemoryUsagePercentage"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = var.redis_memory_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "breaching"

  dimensions = {
    ReplicationGroupId = var.redis_replication_group_id
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "redis_evictions" {
  count = var.redis_replication_group_id != null ? 1 : 0

  alarm_name          = "${var.name}-redis-evictions"
  alarm_description   = "Redis evictions detected (above ${var.redis_evictions_threshold})"
  namespace           = "AWS/ElastiCache"
  metric_name         = "Evictions"
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 2
  threshold           = var.redis_evictions_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"

  dimensions = {
    ReplicationGroupId = var.redis_replication_group_id
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

################################################################################
# ALB Alarms
################################################################################

resource "aws_cloudwatch_metric_alarm" "alb_5xx_errors" {
  count = var.alb_arn_suffix != null ? 1 : 0

  alarm_name          = "${var.name}-alb-5xx-high"
  alarm_description   = "ALB 5xx error count above ${var.alb_5xx_threshold}"
  namespace           = "AWS/ApplicationELB"
  metric_name         = "HTTPCode_ELB_5XX_Count"
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 2
  threshold           = var.alb_5xx_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"

  dimensions = {
    LoadBalancer = var.alb_arn_suffix
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "alb_target_5xx_errors" {
  count = var.alb_arn_suffix != null ? 1 : 0

  alarm_name          = "${var.name}-alb-target-5xx-high"
  alarm_description   = "ALB target 5xx error count above ${var.alb_target_5xx_threshold}"
  namespace           = "AWS/ApplicationELB"
  metric_name         = "HTTPCode_Target_5XX_Count"
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 2
  threshold           = var.alb_target_5xx_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"

  dimensions = {
    LoadBalancer = var.alb_arn_suffix
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "alb_response_time" {
  count = var.alb_arn_suffix != null ? 1 : 0

  alarm_name          = "${var.name}-alb-response-time-high"
  alarm_description   = "ALB target response time above ${var.alb_response_time_threshold}s"
  namespace           = "AWS/ApplicationELB"
  metric_name         = "TargetResponseTime"
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = var.alb_response_time_threshold
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"

  dimensions = {
    LoadBalancer = var.alb_arn_suffix
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "alb_unhealthy_hosts" {
  for_each = var.alb_target_group_arns

  alarm_name          = "${each.key}-unhealthy-hosts"
  alarm_description   = "Unhealthy targets detected for ${each.key}"
  namespace           = "AWS/ApplicationELB"
  metric_name         = "UnHealthyHostCount"
  statistic           = "Maximum"
  period              = 60
  evaluation_periods  = 3
  threshold           = 0
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"

  dimensions = {
    TargetGroup  = each.value.target_group_arn_suffix
    LoadBalancer = var.alb_arn_suffix
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = var.tags
}
