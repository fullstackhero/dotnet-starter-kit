output "task_definition_arn" {
  description = "Full ARN (with revision) of the registered task definition."
  value       = aws_ecs_task_definition.this.arn
}

output "task_definition_family" {
  description = "Task definition family. Pass to `aws ecs run-task --task-definition` to always launch the latest revision."
  value       = aws_ecs_task_definition.this.family
}

output "container_name" {
  description = "Container name (use for `run-task` container overrides)."
  value       = var.name
}

output "security_group_id" {
  description = "Security group ID for the task's awsvpc network configuration."
  value       = aws_security_group.this.id
}

output "log_group_name" {
  description = "CloudWatch log group the task streams to."
  value       = aws_cloudwatch_log_group.this.name
}
