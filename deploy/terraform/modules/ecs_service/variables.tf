################################################################################
# Required Variables
################################################################################

variable "name" {
  type        = string
  description = "Name of the ECS service."

  validation {
    condition     = can(regex("^[a-zA-Z0-9-_]+$", var.name))
    error_message = "Service name must contain only alphanumeric characters, hyphens, and underscores."
  }
}

variable "region" {
  type        = string
  description = "AWS region."
}

variable "cluster_arn" {
  type        = string
  description = "ARN of the ECS cluster."
}

variable "container_image" {
  type        = string
  description = "Container image to deploy."

  validation {
    condition     = length(var.container_image) > 0
    error_message = "Container image must not be empty."
  }
}

variable "container_port" {
  type        = number
  description = "Container port exposed by the service."

  validation {
    condition     = var.container_port > 0 && var.container_port <= 65535
    error_message = "Container port must be between 1 and 65535."
  }
}

variable "cpu" {
  type        = string
  description = "Fargate CPU units (256, 512, 1024, 2048, 4096, 8192, 16384)."

  validation {
    condition     = contains(["256", "512", "1024", "2048", "4096", "8192", "16384"], var.cpu)
    error_message = "CPU must be one of: 256, 512, 1024, 2048, 4096, 8192, 16384."
  }
}

variable "memory" {
  type        = string
  description = "Fargate memory in MiB."
}

variable "vpc_id" {
  type        = string
  description = "VPC ID for the service."
}

variable "vpc_cidr_block" {
  type        = string
  description = "CIDR block of the VPC."
}

variable "subnet_ids" {
  type        = list(string)
  description = "Subnets for ECS tasks."

  validation {
    condition     = length(var.subnet_ids) >= 1
    error_message = "At least one subnet must be specified."
  }
}

variable "listener_arn" {
  type        = string
  description = "ALB listener ARN."
}

variable "listener_rule_priority" {
  type        = number
  description = "Priority for the ALB listener rule."

  validation {
    condition     = var.listener_rule_priority >= 1 && var.listener_rule_priority <= 50000
    error_message = "Listener rule priority must be between 1 and 50000."
  }
}

################################################################################
# Optional Variables - Deployment
################################################################################

variable "desired_count" {
  type        = number
  description = "Desired number of tasks."
  default     = 1

  validation {
    condition     = var.desired_count >= 0
    error_message = "Desired count must be non-negative."
  }
}

variable "assign_public_ip" {
  type        = bool
  description = "Assign public IP to tasks."
  default     = false
}

variable "cpu_architecture" {
  type        = string
  description = "CPU architecture (X86_64 or ARM64)."
  default     = "X86_64"

  validation {
    condition     = contains(["X86_64", "ARM64"], var.cpu_architecture)
    error_message = "CPU architecture must be X86_64 or ARM64."
  }
}

variable "enable_execute_command" {
  type        = bool
  description = "Enable ECS Exec for debugging."
  default     = false
}

variable "use_capacity_provider_strategy" {
  type        = bool
  description = "Use capacity provider strategy instead of launch type. Automatically set to true if use_fargate_spot is true."
  default     = false
}

variable "use_fargate_spot" {
  type        = bool
  description = "Use Fargate Spot capacity (convenience variable that automatically configures capacity provider strategy)."
  default     = false
}

variable "capacity_provider_strategy" {
  type = list(object({
    capacity_provider = string
    weight            = number
    base              = optional(number, 0)
  }))
  description = "Capacity provider strategy (requires use_capacity_provider_strategy = true). Ignored if use_fargate_spot is true."
  default = [
    {
      capacity_provider = "FARGATE"
      weight            = 1
      base              = 1
    }
  ]
}

################################################################################
# Optional Variables - Deployment Strategy
################################################################################

variable "deployment_minimum_healthy_percent" {
  type        = number
  description = "Minimum healthy percent during deployment."
  default     = 100
}

variable "deployment_maximum_percent" {
  type        = number
  description = "Maximum percent during deployment."
  default     = 200
}

variable "enable_circuit_breaker" {
  type        = bool
  description = "Enable deployment circuit breaker."
  default     = true
}

variable "enable_circuit_breaker_rollback" {
  type        = bool
  description = "Enable automatic rollback on deployment failure."
  default     = true
}

variable "wait_for_steady_state" {
  type        = bool
  description = "Wait for the ECS service to reach a steady state after deployment. Recommended for production."
  default     = false
}

################################################################################
# Optional Variables - Health Check
################################################################################

variable "path_patterns" {
  type        = list(string)
  description = "Path patterns for ALB listener rule."
  default     = ["/*"]
}

variable "health_check_path" {
  type        = string
  description = "Health check path for the target group."
  default     = "/"
}

variable "health_check_matcher" {
  type        = string
  description = "HTTP status codes for healthy response."
  default     = "200-399"
}

variable "health_check_interval" {
  type        = number
  description = "Health check interval in seconds."
  default     = 30
}

variable "health_check_timeout" {
  type        = number
  description = "Health check timeout in seconds."
  default     = 5
}

variable "health_check_healthy_threshold" {
  type        = number
  description = "Number of consecutive successful health checks."
  default     = 2
}

variable "health_check_unhealthy_threshold" {
  type        = number
  description = "Number of consecutive failed health checks."
  default     = 5
}

variable "health_check_grace_period_seconds" {
  type        = number
  description = "Seconds to wait before health checks start."
  default     = 60
}

variable "deregistration_delay" {
  type        = number
  description = "Time to wait for in-flight requests before deregistering."
  default     = 30
}

variable "container_health_check" {
  type = object({
    command      = list(string)
    interval     = optional(number, 30)
    timeout      = optional(number, 5)
    retries      = optional(number, 3)
    start_period = optional(number, 60)
  })
  description = "Container health check configuration."
  default     = null
}

################################################################################
# Optional Variables - Security
################################################################################

variable "readonly_root_filesystem" {
  type        = bool
  description = "When true, the container root filesystem is read-only. Requires writable tmpfs mounts for /tmp etc."
  default     = false
}

variable "kms_key_id" {
  type        = string
  description = "KMS key ID for encrypting the CloudWatch log group."
  default     = null
}

################################################################################
# Optional Variables - Logging
################################################################################

variable "log_retention_in_days" {
  type        = number
  description = "CloudWatch log retention in days."
  default     = 30

  validation {
    condition     = contains([0, 1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1096, 1827, 2192, 2557, 2922, 3288, 3653], var.log_retention_in_days)
    error_message = "Log retention must be a valid CloudWatch Logs retention value."
  }
}

################################################################################
# Optional Variables - Environment & Secrets
################################################################################

variable "environment_variables" {
  type        = map(string)
  description = "Plain environment variables for the container. Sensitive values should use the secrets variable instead."
  default     = {}
}

variable "secrets" {
  type = list(object({
    name      = string
    valueFrom = string
  }))
  description = "Secrets from Secrets Manager or Parameter Store."
  default     = []
}

################################################################################
# Optional Variables - IAM
################################################################################

variable "task_role_arn" {
  type        = string
  description = "Optional task role ARN to attach to the task definition."
  default     = null
}

################################################################################
# Auto Scaling
################################################################################

variable "enable_autoscaling" {
  type        = bool
  description = "Enable Application Auto Scaling for the ECS service."
  default     = false
}

variable "autoscaling_min_capacity" {
  type        = number
  description = "Minimum number of tasks when auto-scaling."
  default     = 1

  validation {
    condition     = var.autoscaling_min_capacity >= 1
    error_message = "Minimum capacity must be at least 1."
  }
}

variable "autoscaling_max_capacity" {
  type        = number
  description = "Maximum number of tasks when auto-scaling."
  default     = 10

  validation {
    condition     = var.autoscaling_max_capacity >= 1
    error_message = "Maximum capacity must be at least 1."
  }
}

variable "autoscaling_cpu_target" {
  type        = number
  description = "Target CPU utilization percentage for auto-scaling."
  default     = 70

  validation {
    condition     = var.autoscaling_cpu_target > 0 && var.autoscaling_cpu_target <= 100
    error_message = "CPU target must be between 1 and 100."
  }
}

variable "autoscaling_memory_target" {
  type        = number
  description = "Target memory utilization percentage for auto-scaling."
  default     = 80

  validation {
    condition     = var.autoscaling_memory_target > 0 && var.autoscaling_memory_target <= 100
    error_message = "Memory target must be between 1 and 100."
  }
}

variable "autoscaling_requests_per_target" {
  type        = number
  description = "Target ALB request count per task for auto-scaling. Set to 0 to disable request-based scaling."
  default     = 0
}

variable "autoscaling_scale_in_cooldown" {
  type        = number
  description = "Cooldown period in seconds after a scale-in event."
  default     = 300
}

variable "autoscaling_scale_out_cooldown" {
  type        = number
  description = "Cooldown period in seconds after a scale-out event."
  default     = 60
}

variable "alb_arn_suffix" {
  type        = string
  description = "ALB ARN suffix (required for request-based auto-scaling)."
  default     = ""
}

################################################################################
# Tags
################################################################################

variable "tags" {
  type        = map(string)
  description = "Tags to apply to resources."
  default     = {}
}
