variable "aws_region" {
  type    = string
  default = "us-east-1"
}

variable "cluster_id" {
  type = string
}

variable "service_name" {
  type = string
}

variable "container_name" {
  type = string
}

variable "container_image" {
  type = string
}

variable "container_port" {
  type    = number
  default = 8080
}

variable "desired_count" {
  type    = number
  default = 1
}

variable "cpu" {
  type    = number
  default = 1024
}

variable "memory" {
  type    = number
  default = 2048
}

variable "subnet_ids" {
  type = list(string)
}

variable "environment_variables" {
  type    = map(string)
  default = {}
}

variable "vpc_id" {
}

variable "environment" {
}

variable "entry_point" {
  default = []
  type    = list(string)
}

variable "enable_health_check" {
  type    = bool
  default = true
}

variable "health_check_endpoint" {
  type    = string
  default = "/health"
}

variable "log_retention_period" {
  type    = number
  default = 60
}
