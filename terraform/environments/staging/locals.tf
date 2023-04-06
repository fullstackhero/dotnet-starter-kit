locals {
  common_tags = {
    Environment = var.environment
    Owner       = var.owner
    Project     = var.project_name
  }
}
