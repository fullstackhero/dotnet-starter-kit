variable "vpc_id" {
  type        = string
  description = "VPC id"
  default     = null
}

variable "network_mode" {
  type        = string
  description = "ECS network mode"
  default     = "awsvpc"
}

variable "target_group_arn" {
  type        = string
  description = "Load balancer target group arn"
}
