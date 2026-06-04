# Partial backend configuration — completed per environment at init time:
#   terraform init -backend-config=../envs/<env>/<region>/backend.hcl
# State locking uses S3 native locking (use_lockfile); no DynamoDB table needed.
terraform {
  backend "s3" {}
}
