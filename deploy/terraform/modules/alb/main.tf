################################################################################
# Application Load Balancer
################################################################################

resource "aws_lb" "this" {
  name               = var.name
  internal           = var.internal
  load_balancer_type = "application"
  security_groups    = [var.security_group_id]
  subnets            = var.subnet_ids

  enable_deletion_protection = var.enable_deletion_protection
  enable_http2               = var.enable_http2
  idle_timeout               = var.idle_timeout
  drop_invalid_header_fields = var.drop_invalid_header_fields
  desync_mitigation_mode     = var.desync_mitigation_mode
  preserve_host_header       = var.preserve_host_header
  xff_header_processing_mode = var.xff_header_processing_mode

  dynamic "access_logs" {
    for_each = var.access_logs_bucket != null ? [1] : []
    content {
      bucket  = var.access_logs_bucket
      prefix  = var.access_logs_prefix
      enabled = true
    }
  }

  dynamic "connection_logs" {
    for_each = var.connection_logs_bucket != null ? [1] : []
    content {
      bucket  = var.connection_logs_bucket
      prefix  = var.connection_logs_prefix
      enabled = true
    }
  }

  tags = merge(var.tags, {
    Name = var.name
  })
}

################################################################################
# HTTP Listener
################################################################################

resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.this.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type = var.enable_https ? "redirect" : "fixed-response"

    dynamic "redirect" {
      for_each = var.enable_https ? [1] : []
      content {
        port        = "443"
        protocol    = "HTTPS"
        status_code = "HTTP_301"
      }
    }

    dynamic "fixed_response" {
      for_each = var.enable_https ? [] : [1]
      content {
        content_type = "text/plain"
        message_body = "Not configured"
        status_code  = "404"
      }
    }
  }

  tags = var.tags
}

################################################################################
# HTTPS Listener (Optional)
################################################################################

resource "aws_lb_listener" "https" {
  count = var.enable_https ? 1 : 0

  load_balancer_arn = aws_lb.this.arn
  port              = 443
  protocol          = "HTTPS"
  ssl_policy        = var.ssl_policy
  certificate_arn   = var.certificate_arn

  default_action {
    type = "fixed-response"

    fixed_response {
      content_type = "text/plain"
      message_body = "Not configured"
      status_code  = "404"
    }
  }

  tags = var.tags
}

################################################################################
# Additional Certificates (Optional)
################################################################################

resource "aws_lb_listener_certificate" "additional" {
  for_each = var.enable_https ? toset(var.additional_certificate_arns) : []

  listener_arn    = aws_lb_listener.https[0].arn
  certificate_arn = each.value
}
