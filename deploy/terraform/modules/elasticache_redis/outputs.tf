output "primary_endpoint_address" {
  description = "The primary endpoint address for the Redis replication group"
  value       = aws_elasticache_replication_group.this.primary_endpoint_address
}

output "reader_endpoint_address" {
  description = "The reader endpoint address for the Redis replication group"
  value       = aws_elasticache_replication_group.this.reader_endpoint_address
}

output "port" {
  description = "The Redis port"
  value       = aws_elasticache_replication_group.this.port
}

output "replication_group_id" {
  description = "The ID of the ElastiCache replication group"
  value       = aws_elasticache_replication_group.this.id
}

output "arn" {
  description = "The ARN of the ElastiCache replication group"
  value       = aws_elasticache_replication_group.this.arn
}

output "security_group_id" {
  description = "The ID of the Redis security group"
  value       = aws_security_group.this.id
}

output "subnet_group_name" {
  description = "The name of the ElastiCache subnet group"
  value       = aws_elasticache_subnet_group.this.name
}

output "connection_string" {
  description = "Redis connection string for .NET applications"
  value       = "${aws_elasticache_replication_group.this.primary_endpoint_address}:${aws_elasticache_replication_group.this.port},ssl=True,abortConnect=False"
}
