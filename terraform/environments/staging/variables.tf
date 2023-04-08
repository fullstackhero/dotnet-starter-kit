variable "aws_region" {
  type = string
}

variable "environment" {
  type = string
}
variable "owner" {
  type = string
}

variable "project_name" {
  type = string
}

variable "db_name" {
  type = string
}

variable "pg_username" {
  type      = string
  sensitive = true
}

variable "pg_password" {
  type      = string
  sensitive = true
}

variable "aws_region_a" {
  type = string
}
variable "aws_region_b" {
  type = string
}

variable "cidr" {
  type    = string
  default = "10.0.0.0/16"
}

variable "private_cidr_a" {
  type    = string
  default = "10.0.1.0/24"
}

variable "private_cidr_b" {
  type    = string
  default = "10.0.2.0/24"
}

variable "ecs_cluster_name" {
  type = string
}

variable "enable_health_check" {
  type    = bool
  default = false
}

variable "health_check_endpoint" {
  type    = string
  default = "/api/health"
}


variable "api_service_name" {
  type = string
}

variable "api_container_cpu" {
  type = number
}

variable "api_container_memory" {
  type = number
}

variable "api_image_name" {
  type = string
}
