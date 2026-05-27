################################################################################
# CloudWatch Log Group
################################################################################

resource "aws_cloudwatch_log_group" "this" {
  name              = "/ecs/${var.name}"
  retention_in_days = var.log_retention_in_days
  kms_key_id        = var.kms_key_id

  tags = var.tags
}

################################################################################
# Security Group
################################################################################

resource "aws_security_group" "ecs_service" {
  name        = "${var.name}-ecs"
  description = "Security group for ECS service ${var.name}"
  vpc_id      = var.vpc_id

  tags = merge(var.tags, {
    Name = "${var.name}-ecs-sg"
  })
}

resource "aws_vpc_security_group_ingress_rule" "container" {
  security_group_id = aws_security_group.ecs_service.id
  description       = "Allow traffic to container port from VPC"
  from_port         = var.container_port
  to_port           = var.container_port
  ip_protocol       = "tcp"
  cidr_ipv4         = var.vpc_cidr_block

  tags = var.tags
}

resource "aws_vpc_security_group_egress_rule" "all" {
  security_group_id = aws_security_group.ecs_service.id
  description       = "Allow all outbound traffic"
  ip_protocol       = "-1"
  cidr_ipv4         = "0.0.0.0/0"

  tags = var.tags
}

################################################################################
# Target Group
################################################################################

resource "aws_lb_target_group" "this" {
  name                 = "${var.name}-tg"
  port                 = var.container_port
  protocol             = "HTTP"
  target_type          = "ip"
  vpc_id               = var.vpc_id
  deregistration_delay = var.deregistration_delay

  health_check {
    enabled             = true
    path                = var.health_check_path
    matcher             = var.health_check_matcher
    healthy_threshold   = var.health_check_healthy_threshold
    unhealthy_threshold = var.health_check_unhealthy_threshold
    interval            = var.health_check_interval
    timeout             = var.health_check_timeout
  }

  tags = var.tags

  lifecycle {
    create_before_destroy = true
  }
}

################################################################################
# Listener Rule
################################################################################

resource "aws_lb_listener_rule" "this" {
  listener_arn = var.listener_arn
  priority     = var.listener_rule_priority

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.this.arn
  }

  condition {
    path_pattern {
      values = var.path_patterns
    }
  }

  tags = var.tags
}

################################################################################
# IAM - Task Execution Role
################################################################################

resource "aws_iam_role" "task_execution" {
  name               = "${var.name}-task-execution"
  assume_role_policy = data.aws_iam_policy_document.task_execution_assume.json

  tags = var.tags
}

data "aws_iam_policy_document" "task_execution_assume" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role_policy_attachment" "task_execution" {
  role       = aws_iam_role.task_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_role_policy" "task_execution_secrets" {
  count = length(var.secrets) > 0 ? 1 : 0

  name = "${var.name}-secrets-access"
  role = aws_iam_role.task_execution.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        # Use the secret ARN directly - callers should provide the base secret ARN
        Resource = [for s in var.secrets : s.valueFrom]
      }
    ]
  })
}

################################################################################
# Task Definition
################################################################################

resource "aws_ecs_task_definition" "this" {
  family                   = var.name
  cpu                      = var.cpu
  memory                   = var.memory
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  execution_role_arn       = aws_iam_role.task_execution.arn
  task_role_arn            = var.task_role_arn

  container_definitions = jsonencode([
    merge(
      {
        name      = var.name
        image     = var.container_image
        essential = true

        portMappings = [
          {
            containerPort = var.container_port
            hostPort      = var.container_port
            protocol      = "tcp"
            name          = var.name
          }
        ]

        environment = [
          for k, v in var.environment_variables :
          {
            name  = k
            value = v
          }
        ]

        secrets = length(var.secrets) > 0 ? [
          for s in var.secrets :
          {
            name      = s.name
            valueFrom = s.valueFrom
          }
        ] : []

        logConfiguration = {
          logDriver = "awslogs"
          options = {
            awslogs-group         = aws_cloudwatch_log_group.this.name
            awslogs-region        = var.region
            awslogs-stream-prefix = var.name
          }
        }

        # Security hardening
        readonlyRootFilesystem = var.readonly_root_filesystem

        # Enable init process for proper signal handling (PID 1 zombie reaping)
        linuxParameters = {
          initProcessEnabled = true
        }
      },
      var.container_health_check != null ? {
        healthCheck = {
          command     = var.container_health_check.command
          interval    = var.container_health_check.interval
          timeout     = var.container_health_check.timeout
          retries     = var.container_health_check.retries
          startPeriod = var.container_health_check.start_period
        }
      } : {}
    )
  ])

  runtime_platform {
    cpu_architecture        = var.cpu_architecture
    operating_system_family = "LINUX"
  }

  tags = var.tags
}

################################################################################
# Local Variables
################################################################################

locals {
  # Determine if we should use capacity provider strategy
  use_capacity_providers = var.use_fargate_spot || var.use_capacity_provider_strategy

  # Build the capacity provider strategy based on use_fargate_spot or custom strategy
  effective_capacity_provider_strategy = var.use_fargate_spot ? [
    {
      capacity_provider = "FARGATE_SPOT"
      weight            = 1
      base              = 0
    }
  ] : var.capacity_provider_strategy
}

################################################################################
# ECS Service
################################################################################

resource "aws_ecs_service" "this" {
  name                               = var.name
  cluster                            = var.cluster_arn
  task_definition                    = aws_ecs_task_definition.this.arn
  desired_count                      = var.desired_count
  launch_type                        = local.use_capacity_providers ? null : "FARGATE"
  enable_execute_command             = var.enable_execute_command
  health_check_grace_period_seconds  = var.health_check_grace_period_seconds
  deployment_minimum_healthy_percent = var.deployment_minimum_healthy_percent
  deployment_maximum_percent         = var.deployment_maximum_percent

  dynamic "capacity_provider_strategy" {
    for_each = local.use_capacity_providers ? local.effective_capacity_provider_strategy : []
    content {
      capacity_provider = capacity_provider_strategy.value.capacity_provider
      weight            = capacity_provider_strategy.value.weight
      base              = capacity_provider_strategy.value.base
    }
  }

  network_configuration {
    subnets          = var.subnet_ids
    security_groups  = [aws_security_group.ecs_service.id]
    assign_public_ip = var.assign_public_ip
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.this.arn
    container_name   = var.name
    container_port   = var.container_port
  }

  deployment_circuit_breaker {
    enable   = var.enable_circuit_breaker
    rollback = var.enable_circuit_breaker_rollback
  }

  propagate_tags        = "SERVICE"
  wait_for_steady_state = var.wait_for_steady_state

  lifecycle {
    ignore_changes = [desired_count]
  }

  depends_on = [aws_lb_listener_rule.this]

  tags = var.tags
}

################################################################################
# Application Auto Scaling
################################################################################

resource "aws_appautoscaling_target" "this" {
  count = var.enable_autoscaling ? 1 : 0

  max_capacity       = var.autoscaling_max_capacity
  min_capacity       = var.autoscaling_min_capacity
  resource_id        = "service/${split("/", var.cluster_arn)[1]}/${aws_ecs_service.this.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_appautoscaling_policy" "cpu" {
  count = var.enable_autoscaling ? 1 : 0

  name               = "${var.name}-cpu-scaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.this[0].resource_id
  scalable_dimension = aws_appautoscaling_target.this[0].scalable_dimension
  service_namespace  = aws_appautoscaling_target.this[0].service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value       = var.autoscaling_cpu_target
    scale_in_cooldown  = var.autoscaling_scale_in_cooldown
    scale_out_cooldown = var.autoscaling_scale_out_cooldown
  }
}

resource "aws_appautoscaling_policy" "memory" {
  count = var.enable_autoscaling ? 1 : 0

  name               = "${var.name}-memory-scaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.this[0].resource_id
  scalable_dimension = aws_appautoscaling_target.this[0].scalable_dimension
  service_namespace  = aws_appautoscaling_target.this[0].service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageMemoryUtilization"
    }
    target_value       = var.autoscaling_memory_target
    scale_in_cooldown  = var.autoscaling_scale_in_cooldown
    scale_out_cooldown = var.autoscaling_scale_out_cooldown
  }
}

resource "aws_appautoscaling_policy" "requests" {
  count = var.enable_autoscaling && var.autoscaling_requests_per_target > 0 ? 1 : 0

  name               = "${var.name}-request-scaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.this[0].resource_id
  scalable_dimension = aws_appautoscaling_target.this[0].scalable_dimension
  service_namespace  = aws_appautoscaling_target.this[0].service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ALBRequestCountPerTarget"
      resource_label         = "${var.alb_arn_suffix}/${aws_lb_target_group.this.arn_suffix}"
    }
    target_value       = var.autoscaling_requests_per_target
    scale_in_cooldown  = var.autoscaling_scale_in_cooldown
    scale_out_cooldown = var.autoscaling_scale_out_cooldown
  }
}
