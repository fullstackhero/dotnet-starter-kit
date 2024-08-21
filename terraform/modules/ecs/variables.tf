variable "aws_region" {
  description = "The AWS region to deploy resources in"
  type        = string
  default     = "us-east-1"
}

variable "cluster_id" {
  type = string
}

variable "service_name" {
  description = "The name of the ECS service"
  type        = string
}

variable "container_name" {
  description = "The name of the container"
  type        = string
}

variable "container_image" {
  description = "The container image to use"
  type        = string
}

variable "container_port" {
  type    = number
  default = 8080
}

variable "desired_count" {
  description = "The desired number of tasks"
  type        = number
  default     = 1
}

variable "cpu" {
  description = "The number of cpu units to allocate for the task"
  type        = number
  default     = 256
}

variable "memory" {
  description = "The amount of memory (in MiB) to allocate for the task"
  type        = number
  default     = 512
}

variable "subnet_ids" {
  description = "The subnets to run the ECS service in"
  type        = list(string)
}

variable "environment_variables" {
  description = "A map of environment variables to pass to the container"
  type        = map(string)
  default     = {}
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
