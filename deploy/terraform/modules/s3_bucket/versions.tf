terraform {
  # Floor only — reusable child module. The root config pins the exact
  # Terraform core and provider versions (see app_stack/versions.tf).
  required_version = ">= 1.15"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 6.0"
    }
  }
}
