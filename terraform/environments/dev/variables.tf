variable "aws_region" {
  description = "The AWS region to deploy resources in"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  type    = string
  default = "dev"
}
variable "owner" {
  type = string
}

variable "project_name" {
  type = string
}

variable "repository" {
  type = string
}


locals {
  common_tags = {
    Environment = var.environment
    Owner       = var.owner
    Project     = var.project_name
    Repository  = var.repository
  }
}
