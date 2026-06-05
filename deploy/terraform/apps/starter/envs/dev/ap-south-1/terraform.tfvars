################################################################################
# Dev Environment — Asia Pacific (Mumbai) ap-south-1
################################################################################

environment = "dev"
region      = "ap-south-1"

################################################################################
# Network
#
# Distinct CIDR from us-east-1 (10.10.0.0/16) so the two dev stacks can ever be
# peered without overlap.
################################################################################

vpc_cidr_block = "10.20.0.0/16"

public_subnets = {
  a = { cidr_block = "10.20.0.0/24", az = "ap-south-1a" }
  b = { cidr_block = "10.20.1.0/24", az = "ap-south-1b" }
}

private_subnets = {
  a = { cidr_block = "10.20.10.0/24", az = "ap-south-1a" }
  b = { cidr_block = "10.20.11.0/24", az = "ap-south-1b" }
}

single_nat_gateway = true

enable_s3_endpoint   = true
enable_ecr_endpoints = true
enable_logs_endpoint = true

################################################################################
# S3 — bucket names must be globally unique, so they carry an -aps1 suffix to
# coexist with the us-east-1 dev stack (S3 names are global; a bucket lives in
# exactly one region).
################################################################################

app_s3_bucket_name        = "dev-fsh-app-bucket-aps1"
app_s3_enable_public_read = false
app_s3_enable_cloudfront  = true

################################################################################
# Frontend SPAs (S3 + CloudFront) — bucket names must be globally unique
################################################################################

dashboard_s3_bucket_name = "dev-fsh-dashboard-aps1"
admin_s3_bucket_name     = "dev-fsh-admin-aps1"
dashboard_demo_mode      = true

# HTTPS for the API without a custom domain: front the ALB with CloudFront
# (free *.cloudfront.net cert) so the HTTPS SPAs can call it (no mixed content).
enable_api_cloudfront = true

################################################################################
# Database
################################################################################

db_name                        = "fshdb"
db_username                    = "fshadmin"
db_manage_master_user_password = true

################################################################################
# Container Images
#
# Final image refs are "<registry>/<image_name>:<tag>", e.g.
#   ghcr.io/fullstackhero/fsh-api:<tag>
#   ghcr.io/fullstackhero/fsh-db-migrator:<tag>
# The tag is SHARED by both images (built together from the same commit) and must
# be immutable. CI publishes "dev-<full-sha>"; `deploy.sh --build-api` publishes a
# bare 12-char short SHA. Set the tag to whichever was actually pushed to GHCR.
################################################################################

container_registry  = "ghcr.io/fullstackhero"
api_image_name      = "fsh-api"
migrator_image_name = "fsh-db-migrator"
container_image_tag = "dev-ba3e9498632df227a04a633e3573b646c9c2aa62"

################################################################################
# DbMigrator — dev migrates AND seeds (admin + default tenant) on every deploy.
# Seeding is idempotent; Seed:* config comes from the image's appsettings.Development.json.
# Override explicitly via seed_default_admin_password / seed_demo_password if needed.
# Demo tenants (acme/globex) are opt-in: run deploy with --seed-demo / -SeedDemo.
################################################################################

migrator_command = ["apply", "--seed"]

################################################################################
# Services (Fargate Spot for cost savings)
################################################################################

api_cpu              = "1024" # 1 vCPU
api_memory           = "2048" # 2 GB — Fargate's minimum memory for 1 vCPU
api_desired_count    = 1
api_use_fargate_spot = true
