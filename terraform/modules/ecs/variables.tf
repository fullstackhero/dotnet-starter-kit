variable "aws_region" {
  description = "The AWS region to deploy resources in"
  type        = string
  default     = "us-west-2"
}

variable "cluster_name" {
  description = "The name of the ECS cluster"
  type        = string
  default     = "ecs-cluster"
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
  description = "The port that the container will listen on"
  type        = number
  default     = 80
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

variable "subnets" {
  description = "The subnets to run the ECS service in"
  type        = list(string)
}

variable "security_groups" {
  description = "The security groups to associate with the ECS service"
  type        = list(string)
}

variable "environment" {
  description = "A map of environment variables to pass to the container"
  type        = map(string)
  default     = {}
}