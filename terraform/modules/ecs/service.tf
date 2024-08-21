resource "aws_ecs_service" "this" {
  name                 = var.service_name
  cluster              = var.cluster_id
  task_definition      = aws_ecs_task_definition.this.arn
  desired_count        = var.desired_count
  launch_type          = "FARGATE"
  force_new_deployment = true
  network_configuration {
    subnets          = var.subnet_ids
    security_groups  = [aws_security_group.this.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.this.arn
    container_name   = var.container_name
    container_port   = var.container_port
  }
}

resource "aws_security_group" "this" {
  name   = "${var.service_name}-sg"
  vpc_id = var.vpc_id
  ingress {
    protocol    = "tcp"
    from_port   = var.container_port
    to_port     = var.container_port
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_lb" "this" {
  name               = "${var.service_name}-lb"
  internal           = false
  load_balancer_type = "application"
  subnets            = var.subnet_ids

  security_groups = [aws_security_group.this.id]
}

resource "aws_lb_target_group" "this" {
  health_check {
    enabled  = var.enable_health_check
    path     = var.health_check_endpoint
    interval = 30
  }
  name        = "${var.service_name}-tg"
  port        = var.container_port
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = var.vpc_id
}

resource "aws_lb_listener" "this" {
  load_balancer_arn = aws_lb.this.arn
  port              = var.container_port
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.this.arn
  }
}
