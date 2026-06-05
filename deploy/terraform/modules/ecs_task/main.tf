################################################################################
# One-shot ECS (Fargate) task — registers a runnable task definition only.
#
# Unlike the ecs_service module there is NO service, ALB target group, listener
# rule, or autoscaling: this is a job you invoke on demand with `aws ecs
# run-task` (see the deploy scripts) and wait for to exit. Used for the DB
# migrator (apply / apply --seed).
################################################################################

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
# Security Group (egress only — the task makes outbound calls to RDS / Redis /
# Secrets Manager / the registry; nothing connects in to it).
################################################################################

resource "aws_security_group" "this" {
  name        = "${var.name}-ecs"
  description = "Security group for one-shot ECS task ${var.name}"
  vpc_id      = var.vpc_id

  tags = merge(var.tags, {
    Name = "${var.name}-ecs-sg"
  })
}

resource "aws_vpc_security_group_egress_rule" "all" {
  security_group_id = aws_security_group.this.id
  description       = "Allow all outbound traffic"
  ip_protocol       = "-1"
  cidr_ipv4         = "0.0.0.0/0"

  tags = var.tags
}

################################################################################
# IAM - Task Execution Role
################################################################################

data "aws_iam_policy_document" "task_execution_assume" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "task_execution" {
  name               = "${var.name}-task-execution"
  assume_role_policy = data.aws_iam_policy_document.task_execution_assume.json

  tags = var.tags
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
        Effect   = "Allow"
        Action   = ["secretsmanager:GetSecretValue"]
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
    {
      name      = var.name
      image     = var.container_image
      essential = true

      command = var.command

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

      readonlyRootFilesystem = var.readonly_root_filesystem

      # Init process for proper signal handling (PID 1 zombie reaping).
      linuxParameters = {
        initProcessEnabled = true
      }
    }
  ])

  runtime_platform {
    cpu_architecture        = var.cpu_architecture
    operating_system_family = "LINUX"
  }

  tags = var.tags
}
