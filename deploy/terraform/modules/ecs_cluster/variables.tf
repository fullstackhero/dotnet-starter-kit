variable "name" {
  type        = string
  description = "Name of the ECS cluster."

  validation {
    condition     = can(regex("^[a-zA-Z0-9-_]+$", var.name))
    error_message = "Cluster name must contain only alphanumeric characters, hyphens, and underscores."
  }
}

variable "container_insights" {
  type        = bool
  description = "Enable CloudWatch Container Insights for the cluster."
  default     = true
}

variable "capacity_providers" {
  type        = list(string)
  description = "List of capacity providers to associate with the cluster."
  default     = ["FARGATE", "FARGATE_SPOT"]
}

variable "default_capacity_provider_strategy" {
  type = list(object({
    capacity_provider = string
    weight            = number
    base              = optional(number, 0)
  }))
  description = "Default capacity provider strategy for the cluster."
  default = [
    {
      capacity_provider = "FARGATE"
      weight            = 1
      base              = 1
    }
  ]
}

variable "enable_execute_command_logging" {
  type        = bool
  description = "Enable logging for ECS Exec commands."
  default     = false
}

variable "log_retention_in_days" {
  type        = number
  description = "Number of days to retain execute command logs."
  default     = 30

  validation {
    condition     = contains([0, 1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1096, 1827, 2192, 2557, 2922, 3288, 3653], var.log_retention_in_days)
    error_message = "Log retention must be a valid CloudWatch Logs retention value."
  }
}

variable "kms_key_id" {
  type        = string
  description = "KMS key ID for encrypting CloudWatch log groups."
  default     = null
}

variable "tags" {
  type        = map(string)
  description = "Tags to apply to the ECS cluster."
  default     = {}
}
