output "api_url" {
  value       = "http://${aws_lb.fsh_api_alb.dns_name}/api"
  description = "API URL"
}
