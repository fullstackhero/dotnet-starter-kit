# Partial backend configuration.
# Complete it per environment using: terraform init -backend-config=../envs/<env>/<region>/backend.hcl
terraform {
  backend "s3" {}
}
