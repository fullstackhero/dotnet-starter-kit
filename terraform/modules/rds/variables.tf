variable "environment" {
}

variable "subnet_ids" {
  type = list(string)
}

variable "vpc_id" {
}

variable "allocated_storage" {
  default = 10
}

variable "instance_class" {
  default = "db.t3.micro"
}

variable "multi_az" {
  default = false
}

variable "database_name" {
}

variable "database_username" {
  default = "superuser"
}

variable "database_password" {
  default = "123Pa$$word!"
}

variable "cidr_block" {
}

variable "backup_retention_period" {
  default = 10
}
