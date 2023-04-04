variable "aws_region" {
  type    = string
  default = "ap-south-1"
}

variable "common_tags" {
  default = {
    Environment = "staging"
    Owner       = "Mukesh Murugan"
    Project     = "fsh/dotnet-webapi"
  }
  type = map(string)
}

variable "db_name" {
  type    = string
  default = "fshdb"
}

variable "pg_username" {
  type      = string
  default   = "posgresqladmin"
  sensitive = true
}

variable "pg_password" {
  type      = string
  default   = "posgresqladmin"
  sensitive = true
}

variable "region" {
  type    = string
  default = "ap-south-1"
}

variable "region_a" {
  type    = string
  default = "ap-south-1a"
}
variable "region_b" {
  type    = string
  default = "ap-south-1b"
}

variable "cidr" {
  description = "CIDR range for created VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "private_cidr_a" {
  description = "CIDR range for created VPC"
  type        = string
  default     = "10.0.1.0/24"
}

variable "private_cidr_b" {
  description = "CIDR range for created VPC"
  type        = string
  default     = "10.0.2.0/24"
}
