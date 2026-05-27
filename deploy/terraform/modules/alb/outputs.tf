output "arn" {
  description = "The ARN of the load balancer"
  value       = aws_lb.this.arn
}

output "id" {
  description = "The ID of the load balancer"
  value       = aws_lb.this.id
}

output "dns_name" {
  description = "The DNS name of the load balancer"
  value       = aws_lb.this.dns_name
}

output "zone_id" {
  description = "The canonical hosted zone ID of the load balancer"
  value       = aws_lb.this.zone_id
}

output "http_listener_arn" {
  description = "The ARN of the HTTP listener"
  value       = aws_lb_listener.http.arn
}

output "https_listener_arn" {
  description = "The ARN of the HTTPS listener (if enabled)"
  value       = var.enable_https ? aws_lb_listener.https[0].arn : null
}

output "listener_arn" {
  description = "The ARN of the primary listener (HTTPS if enabled, otherwise HTTP)"
  value       = var.enable_https ? aws_lb_listener.https[0].arn : aws_lb_listener.http.arn
}

output "arn_suffix" {
  description = "The ARN suffix of the load balancer (for auto-scaling and CloudWatch)"
  value       = aws_lb.this.arn_suffix
}
