output "endpoint" {
  description = "The connection endpoint address"
  value       = aws_db_instance.this.address
}

output "port" {
  description = "The database port"
  value       = aws_db_instance.this.port
}

output "identifier" {
  description = "The RDS instance identifier"
  value       = aws_db_instance.this.identifier
}

output "arn" {
  description = "The ARN of the RDS instance"
  value       = aws_db_instance.this.arn
}

output "db_name" {
  description = "The database name"
  value       = aws_db_instance.this.db_name
}

output "security_group_id" {
  description = "The ID of the RDS security group"
  value       = aws_security_group.this.id
}

output "db_subnet_group_name" {
  description = "The name of the DB subnet group"
  value       = aws_db_subnet_group.this.name
}

output "connection_string" {
  description = "PostgreSQL connection string (without password)"
  value       = "Host=${aws_db_instance.this.address};Port=${aws_db_instance.this.port};Database=${aws_db_instance.this.db_name};Username=${var.username}"
  sensitive   = true
}

output "secret_arn" {
  description = "The ARN of the Secrets Manager secret (if managed password is enabled)"
  value       = var.manage_master_user_password ? aws_db_instance.this.master_user_secret[0].secret_arn : null
}
