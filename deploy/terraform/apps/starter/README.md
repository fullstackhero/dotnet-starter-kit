# Playground App Stack
Terraform stack for the Playground API. Uses shared modules from `../../modules`.

- Env/region stacks live under `envs/<env>/<region>/` (backend.tf + *.tfvars + main.tf).
- App composition lives under `app_stack/` (wiring ECS services, ALB, RDS, Redis, S3).
- Images are built from GitHub Actions, pushed to ECR, and referenced in tfvars.
