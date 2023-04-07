resource "aws_lb" "fsh_api_alb" {
  name               = "api-alb"
  load_balancer_type = "application"
  security_groups    = [aws_security_group.lb.id]
  subnets            = [aws_subnet.private_east_b.id, aws_subnet.private_east_a.id]
}

resource "aws_lb_target_group" "fsh_api_tg" {
  health_check {
    enabled  = var.enable_health_check
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
