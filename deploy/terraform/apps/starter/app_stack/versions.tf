terraform {
  # Root config — pin Terraform core and lock the AWS provider to the 6.x
  # line. Commit the generated .terraform.lock.hcl so every operator and CI
  # run resolves identical provider builds.
  required_version = ">= 1.15.4"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.46"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}
