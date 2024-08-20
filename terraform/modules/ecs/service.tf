resource "aws_ecs_service" "this" {
  name            = var.service_name
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.this.arn
  desired_count   = var.desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = var.subnets
    security_groups  = var.security_groups
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.this.arn
    container_name   = var.container_name
    container_port   = var.container_port
  }
}

resource "aws_lb" "this" {
  name               = "${var.service_name}-lb"
  internal           = false
  load_balancer_type = "application"
  subnets            = var.subnets

  security_groups = var.security_groups
}

resource "aws_lb_target_group" "this" {
  name     = "${var.service_name}-tg"
  port     = var.container_port
  protocol = "HTTP"
  vpc_id   = data.aws_vpc.this.id
}

resource "aws_lb_listener" "this" {
  load_balancer_arn = aws_lb.this.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.this.arn
  }
}

data "aws_vpc" "this" {
  id = aws_subnet.main[0].vpc_id
}
