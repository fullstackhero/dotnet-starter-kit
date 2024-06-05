resource "aws_vpc" "project_ecs" {
  cidr_block           = var.cidr
  enable_dns_hostnames = true
  enable_dns_support   = true
}

resource "aws_subnet" "private_east_a" {
  vpc_id            = aws_vpc.project_ecs.id
  cidr_block        = var.private_cidr_a
  availability_zone = var.aws_region_a
}

resource "aws_subnet" "private_east_b" {
  vpc_id            = aws_vpc.project_ecs.id
  cidr_block        = var.private_cidr_b
  availability_zone = var.aws_region_b
}

resource "aws_db_subnet_group" "default" {
  name       = "main"
  subnet_ids = [aws_subnet.private_east_b.id, aws_subnet.private_east_a.id]
}


resource "aws_lb" "fsh_api_alb" {
  name               = "fullstackhero-webapi"
  load_balancer_type = "application"
  security_groups    = [aws_security_group.lb.id]
  subnets            = [aws_subnet.private_east_b.id, aws_subnet.private_east_a.id]
}

resource "aws_lb_target_group" "fsh_api_tg" {
  health_check {
    enabled  = true
    path     = var.health_check_endpoint
    interval = 300
  }
  name        = "api-tg"
  port        = 80
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = aws_vpc.project_ecs.id
}

resource "aws_lb_listener" "fsh_listener" {
  load_balancer_arn = aws_lb.fsh_api_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    target_group_arn = aws_lb_target_group.fsh_api_tg.arn
    type             = "forward"
  }
}
