resource "aws_ecs_task_definition" "this" {
  family                   = var.service_name
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.cpu
  memory                   = var.memory
  task_role_arn            = aws_iam_role.ecs_task_role.arn
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn
  container_definitions = jsonencode([{
    name  = var.container_name
    image = var.container_image
    portMappings = [{
      containerPort = var.container_port
      hostPort      = var.container_port
    }]
    essential = true,
    logConfiguration = {
      logDriver = "awslogs",
      options = {
        awslogs-region        = var.aws_region,
        awslogs-group         = "${var.environment}/${var.service_name}",
        awslogs-stream-prefix = "${var.service_name}"
      }
    },
    entryPoint = var.entry_point
    environment = [
      for key, value in var.environment_variables : {
        name  = key
        value = value
      }
    ]
  }])
}

resource "aws_cloudwatch_log_group" "this" {
  name              = "${var.environment}/${var.service_name}"
  retention_in_days = var.log_retention_period
}
