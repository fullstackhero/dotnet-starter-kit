variable "name" {
  type        = string
  description = "Name prefix for networking resources."

  validation {
    condition     = can(regex("^[a-z0-9-]+$", var.name))
    error_message = "Name must contain only lowercase letters, numbers, and hyphens."
  }
}

variable "cidr_block" {
  type        = string
  description = "CIDR block for the VPC."

  validation {
    condition     = can(cidrhost(var.cidr_block, 0))
    error_message = "Must be a valid CIDR block."
  }
}

variable "public_subnets" {
  description = "Map of public subnet definitions."
  type = map(object({
    cidr_block = string
    az         = string
  }))

  validation {
    condition     = length(var.public_subnets) >= 1
    error_message = "At least one public subnet must be defined."
  }
}

variable "private_subnets" {
  description = "Map of private subnet definitions."
  type = map(object({
    cidr_block = string
    az         = string
  }))

  validation {
    condition     = length(var.private_subnets) >= 1
    error_message = "At least one private subnet must be defined."
  }
}

variable "tags" {
  type        = map(string)
  description = "Tags to apply to networking resources."
  default     = {}
}

################################################################################
# NAT Gateway Options
################################################################################

variable "enable_nat_gateway" {
  type        = bool
  description = "Enable NAT Gateway for private subnets."
  default     = true
}

variable "single_nat_gateway" {
  type        = bool
  description = "Use a single NAT Gateway for all private subnets (cost saving for non-prod)."
  default     = false
}

################################################################################
# VPC Endpoints (Cost Optimization)
################################################################################

variable "enable_s3_endpoint" {
  type        = bool
  description = "Enable S3 Gateway endpoint (free, reduces NAT costs)."
  default     = true
}

variable "enable_ecr_endpoints" {
  type        = bool
  description = "Enable ECR Interface endpoints for private container pulls."
  default     = false
}

variable "enable_logs_endpoint" {
  type        = bool
  description = "Enable CloudWatch Logs Interface endpoint."
  default     = false
}

variable "enable_secretsmanager_endpoint" {
  type        = bool
  description = "Enable Secrets Manager Interface endpoint."
  default     = false
}

################################################################################
# Flow Logs
################################################################################

variable "enable_flow_logs" {
  type        = bool
  description = "Enable VPC Flow Logs."
  default     = false
}

variable "kms_key_id" {
  type        = string
  description = "KMS key ID for encrypting CloudWatch log groups. Uses default AWS key if not specified."
  default     = null
}

variable "flow_logs_retention_days" {
  type        = number
  description = "Number of days to retain flow logs in CloudWatch."
  default     = 14

  validation {
    condition     = contains([0, 1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1096, 1827, 2192, 2557, 2922, 3288, 3653], var.flow_logs_retention_days)
    error_message = "Flow logs retention must be a valid CloudWatch Logs retention value."
  }
}
