resource "aws_ecs_task_definition" "this" {
  family                   = var.service_name
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.cpu
  memory                   = var.memory

  container_definitions = jsonencode([{
    name  = var.container_name
    image = var.container_image
    portMappings = [{
      containerPort = var.container_port
      hostPort      = var.container_port
    }]
    environment = [
      for key, value in var.environment : {
        name  = key
        value = value
      }
    ]
  }])
}
