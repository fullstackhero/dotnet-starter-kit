output "id" {
  description = "The ID of the ECS cluster"
  value       = aws_ecs_cluster.this.id
}

output "arn" {
  description = "The ARN of the ECS cluster"
  value       = aws_ecs_cluster.this.arn
}

output "name" {
  description = "The name of the ECS cluster"
  value       = aws_ecs_cluster.this.name
}
