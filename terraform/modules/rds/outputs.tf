output "db_instance_address" {
  value = aws_db_instance.this.address
}

output "db_instance_id" {
  value = aws_db_instance.this.id
}

output "connection_string" {
  value = "Server=${aws_db_instance.this.address};Port=5432;Database=${var.database_name};User Id=${var.database_username};Password=${var.database_password}"
}
