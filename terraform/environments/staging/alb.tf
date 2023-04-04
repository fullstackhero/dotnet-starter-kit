resource "aws_lb" "fsh_api_alb" {
  name               = "fsh-api-alb"
  load_balancer_type = "application"
  tags               = merge(var.common_tags)
  security_groups    = [aws_security_group.lb.id]
  subnets            = [aws_subnet.private_east_b.id, aws_subnet.private_east_a.id]
}

resource "aws_lb_target_group" "fsh_api_tg" {
  health_check {
    enabled = true
    path    = "/api/health"
  }
  name        = "fsh-api-tg"
  port        = 80
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = aws_vpc.project_ecs.id
  tags        = merge(var.common_tags)
}

resource "aws_lb_listener" "fsh_listener" {
  load_balancer_arn = aws_lb.fsh_api_alb.arn
  port              = "80"
  protocol          = "HTTP"
  tags              = merge(var.common_tags)
  default_action {
    target_group_arn = aws_lb_target_group.fsh_api_tg.arn
    type             = "forward"
  }
}
