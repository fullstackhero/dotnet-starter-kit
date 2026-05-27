output "web_acl_arn" {
  description = "The ARN of the WAF Web ACL"
  value       = aws_wafv2_web_acl.this.arn
}

output "web_acl_id" {
  description = "The ID of the WAF Web ACL"
  value       = aws_wafv2_web_acl.this.id
}

output "web_acl_capacity" {
  description = "The web ACL capacity units (WCU) currently being used"
  value       = aws_wafv2_web_acl.this.capacity
}

output "log_group_arn" {
  description = "The ARN of the WAF CloudWatch log group (if logging enabled)"
  value       = var.enable_logging ? aws_cloudwatch_log_group.waf[0].arn : null
}
