################################################################################
# Required Variables
################################################################################

variable "name" {
  type        = string
  description = "Name of the ECS task (family + log group + security group)."

  validation {
    condition     = can(regex("^[a-zA-Z0-9-_]+$", var.name))
    error_message = "Task name must contain only alphanumeric characters, hyphens, and underscores."
  }
}

variable "region" {
  type        = string
  description = "AWS region (used for the awslogs log driver)."
}

variable "vpc_id" {
  type        = string
  description = "VPC ID the task's security group lives in."
}

variable "container_image" {
  type        = string
  description = "Container image to run."

  validation {
    condition     = length(var.container_image) > 0
    error_message = "Container image must not be empty."
  }
}

################################################################################
# Optional Variables - Task Sizing
################################################################################

variable "cpu" {
  type        = number
  description = "Fargate task CPU units."
  default     = 512
}

variable "memory" {
  type        = number
  description = "Fargate task memory (MiB)."
  default     = 1024
}

variable "cpu_architecture" {
  type        = string
  description = "CPU architecture for the task (X86_64 or ARM64)."
  default     = "X86_64"

  validation {
    condition     = contains(["X86_64", "ARM64"], var.cpu_architecture)
    error_message = "cpu_architecture must be X86_64 or ARM64."
  }
}

################################################################################
# Optional Variables - Runtime
################################################################################

variable "command" {
  type        = list(string)
  description = "Command (args) passed to the container entrypoint. Empty uses the image default."
  default     = []
}

variable "environment_variables" {
  type        = map(string)
  description = "Plain environment variables. Sensitive values should use the secrets variable instead."
  default     = {}
}

variable "secrets" {
  type = list(object({
    name      = string
    valueFrom = string
  }))
  description = "Secrets from Secrets Manager / Parameter Store, injected by the task execution role."
  default     = []
}

variable "task_role_arn" {
  type        = string
  description = "Optional task role ARN (app-level AWS permissions). Null = no task role."
  default     = null
}

variable "readonly_root_filesystem" {
  type        = bool
  description = "Mount the container root filesystem read-only."
  default     = false
}

################################################################################
# Optional Variables - Logging & Tagging
################################################################################

variable "log_retention_in_days" {
  type        = number
  description = "CloudWatch log group retention."
  default     = 30
}

variable "kms_key_id" {
  type        = string
  description = "Optional KMS key ARN for log group encryption."
  default     = null
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to created resources."
  default     = {}
}
