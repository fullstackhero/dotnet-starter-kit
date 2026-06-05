# Starter App Stack

Terraform root config for the FullStackHero Starter Kit on AWS. Composes the
shared modules in `../../modules`.

What it provisions:

- **API** — ECS Fargate service behind an ALB (+ WAF, RDS PostgreSQL, ElastiCache Redis, app S3 bucket).
- **DbMigrator** — a one-shot ECS Fargate task definition (`modules/ecs_task`, no service). The deploy scripts run it with `aws ecs run-task` after `apply` and wait for exit 0. Command is per-environment: dev runs `apply --seed`, prod runs `apply` (migrate only).
- **Dashboard SPA** (`clients/dashboard`) — private S3 + CloudFront (OAC), tenant-facing.
- **Admin SPA** (`clients/admin`) — private S3 + CloudFront (OAC), operator-facing.

The two React SPAs replace the old server-rendered Blazor service. Terraform owns
each SPA's `config.json` so the API/dashboard URLs are wired without rebuilding.

## Layout

- `app_stack/` — the root config (provider, backend, composition, variables, outputs). Run Terraform from here.
- `envs/<env>/<region>/` — per-environment `backend.hcl` + `terraform.tfvars`, at the app root (referenced from `app_stack/` as `../envs/...`).

> The previous two-layer `shared/` → `app_stack/` indirection was collapsed into
> this single root. Migrating an existing deployment? Re-init `app_stack/` against
> the same backend key; resource addresses lose the `module.app.` prefix, so run
> `terraform state mv module.app.module.network module.network` (etc.) once, or
> `terraform plan` and reconcile with `moved {}` blocks.

## One-command deploy

Provisions infra **and** publishes the API image + both SPAs in a single step
(`deploy.sh` for bash/CI/macOS/Linux, `deploy.ps1` for Windows):

The region is **required** — these scripts never assume one. Pass it as the
2nd positional arg (bash) or `-Region` (PowerShell); omit it and they prompt.

```bash
# bash
./deploy.sh dev us-east-1                       # use the image tag from tfvars; dev auto-seeds (apply --seed)
./deploy.sh dev ap-south-1 --seed-demo           # also seed acme/globex demo tenants
./deploy.sh prod us-east-1 --build-api --auto-approve   # also build+push the API + migrator images at the current git SHA
```

```powershell
# PowerShell
./deploy.ps1 -Environment dev -Region us-east-1
./deploy.ps1 -Environment dev -Region ap-south-1 -SeedDemo
./deploy.ps1 -Environment prod -Region us-east-1 -BuildApi -AutoApprove
```

The script: `terraform init` + `apply` → (optional) build & push the API +
migrator containers → **run the DbMigrator task and wait for exit 0** →
`npm run build` each SPA → `aws s3 sync` to its bucket → CloudFront invalidation.
Requires Terraform >= 1.15.4, the AWS CLI (configured), `jq`, and Node;
`--build-api` additionally needs the .NET SDK + a registry login. State backend
is bootstrapped once via `../../bootstrap`.

**Migration flags:** `--skip-migrate` / `-SkipMigrate` skips the migrator run;
`--seed-demo` / `-SeedDemo` runs the migrator's `seed-demo` verb after migrating.
The migrator image must be published **and public** in GHCR at the deployed tag
(see the repo's `backend.yml` publish jobs) before the task can pull it.

## Manual Terraform (if you prefer)

```bash
cd deploy/terraform/apps/starter/app_stack
terraform init  -backend-config=../envs/dev/us-east-1/backend.hcl
terraform apply -var-file=../envs/dev/us-east-1/terraform.tfvars
```

Publishing an SPA build by hand (note `--exclude config.json` — Terraform owns
the runtime config so the API/dashboard URLs stay authoritative):

```bash
aws s3 sync clients/dashboard/dist "s3://$(terraform output -json dashboard_site | jq -r .bucket_name)" --delete --exclude config.json
aws cloudfront create-invalidation --distribution-id "$(terraform output -json dashboard_site | jq -r .cloudfront_distribution_id)" --paths '/*'
```
