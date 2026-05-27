output "service_id" {
  description = "The ID of the ECS service"
  value       = aws_ecs_service.this.id
}

output "service_name" {
  description = "The name of the ECS service"
  value       = aws_ecs_service.this.name
}

output "task_definition_arn" {
  description = "The ARN of the task definition"
  value       = aws_ecs_task_definition.this.arn
}

output "task_definition_family" {
  description = "The family of the task definition"
  value       = aws_ecs_task_definition.this.family
}

output "security_group_id" {
  description = "The ID of the ECS service security group"
  value       = aws_security_group.ecs_service.id
}

output "target_group_arn" {
  description = "The ARN of the target group"
  value       = aws_lb_target_group.this.arn
}

output "cloudwatch_log_group_name" {
  description = "The name of the CloudWatch log group"
  value       = aws_cloudwatch_log_group.this.name
}

output "cloudwatch_log_group_arn" {
  description = "The ARN of the CloudWatch log group"
  value       = aws_cloudwatch_log_group.this.arn
}

output "target_group_arn_suffix" {
  description = "The ARN suffix of the target group (for CloudWatch alarms)"
  value       = aws_lb_target_group.this.arn_suffix
}

output "execution_role_arn" {
  description = "The ARN of the task execution role"
  value       = aws_iam_role.task_execution.arn
}
