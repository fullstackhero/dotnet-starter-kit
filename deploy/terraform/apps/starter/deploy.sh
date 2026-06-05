#!/usr/bin/env bash
#
# One-command deploy of the FullStackHero Starter Kit to AWS.
#
#   ./deploy.sh <dev|staging|prod> <region>
#
# Region is required — the script never assumes one. Pass it as the 2nd arg
# (e.g. `./deploy.sh dev ap-south-1`) or it will prompt interactively.
#
# It will, in order:
#   1. terraform init + apply (infra: VPC, ALB+WAF, ECS API, RDS, Redis, S3, the two SPA CloudFront sites)
#   2. (optional) build & push the API + DbMigrator container images
#   3. run the DbMigrator one-shot ECS task (apply / apply --seed) and wait for exit 0
#   4. build the React apps and publish them to their S3 buckets + invalidate CloudFront
#
# Flags:
#   --build-api          Build & push the API + migrator images at the current git SHA and deploy that tag.
#   --image-tag TAG      Deploy a specific, already-published image tag.
#   --registry REG       Container registry (default: ghcr.io/fullstackhero). Used only with --build-api.
#   --skip-migrate       Skip running the DbMigrator task after apply.
#   --seed-demo          After migrating, also run the migrator's `seed-demo` verb (acme/globex demo tenants).
#   --skip-frontend      Skip building/publishing the SPAs.
#   --auto-approve       Don't prompt before applying.
#
# Requires: terraform >= 1.15.4, aws cli (configured creds), jq, node/npm, and
# (with --build-api) the .NET SDK + a registry login.
set -euo pipefail

MIN_TF_VERSION="1.15.4"
DEFAULT_REGISTRY="ghcr.io/fullstackhero"
API_IMAGE_NAME="fsh-api"
MIGRATOR_IMAGE_NAME="fsh-db-migrator"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"
APP_STACK_DIR="$SCRIPT_DIR/app_stack"

# ---- args -------------------------------------------------------------------
ENVIRONMENT="${1:-}"
REGION=""
BUILD_API=false
SKIP_FRONTEND=false
SKIP_MIGRATE=false
SEED_DEMO=false
AUTO_APPROVE=false
IMAGE_TAG=""
REGISTRY="$DEFAULT_REGISTRY"

shift || true
while [[ $# -gt 0 ]]; do
  case "$1" in
    --build-api) BUILD_API=true ;;
    --skip-frontend) SKIP_FRONTEND=true ;;
    --skip-migrate) SKIP_MIGRATE=true ;;
    --seed-demo) SEED_DEMO=true ;;
    --auto-approve) AUTO_APPROVE=true ;;
    --image-tag) IMAGE_TAG="${2:-}"; shift ;;
    --registry) REGISTRY="${2:-}"; shift ;;
    dev|staging|prod) ENVIRONMENT="$1" ;;
    *) if [[ "$1" =~ ^[a-z]{2}-[a-z]+-[0-9]$ ]]; then REGION="$1"; else echo "Unknown argument: $1" >&2; exit 2; fi ;;
  esac
  shift
done

die() { echo "ERROR: $*" >&2; exit 1; }

[[ "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]] || die "usage: ./deploy.sh <dev|staging|prod> <region> [flags]"

# Region is required — never assume one. Prompt when interactive, else fail.
if [[ -z "$REGION" ]]; then
  if [[ -t 0 ]]; then
    read -rp "AWS region (e.g. us-east-1, ap-south-1): " REGION
  else
    die "no region given — pass it as the 2nd arg (e.g. ./deploy.sh $ENVIRONMENT us-east-1); refusing to assume a default"
  fi
fi
[[ "$REGION" =~ ^[a-z]{2}-[a-z]+-[0-9]$ ]] || die "invalid AWS region: '$REGION'"

ENV_DIR="$SCRIPT_DIR/envs/$ENVIRONMENT/$REGION"
[[ -f "$ENV_DIR/backend.hcl" ]] || die "no backend.hcl for $ENVIRONMENT/$REGION at $ENV_DIR"
[[ -f "$ENV_DIR/terraform.tfvars" ]] || die "no terraform.tfvars for $ENVIRONMENT/$REGION at $ENV_DIR"

# ---- tooling preflight ------------------------------------------------------
for tool in terraform aws jq; do command -v "$tool" >/dev/null || die "$tool is required but not installed"; done

TF_VERSION="$(terraform version -json | jq -r .terraform_version)"
if [[ "$(printf '%s\n%s\n' "$MIN_TF_VERSION" "$TF_VERSION" | sort -V | head -1)" != "$MIN_TF_VERSION" ]]; then
  die "Terraform >= $MIN_TF_VERSION required (found $TF_VERSION). Upgrade with e.g. 'choco upgrade terraform'."
fi

echo "==> Deploying '$ENVIRONMENT' in $REGION"

# ---- 1. optional API image build/push --------------------------------------
TF_IMAGE_ARGS=()
if [[ "$BUILD_API" == true ]]; then
  [[ -n "$IMAGE_TAG" ]] || IMAGE_TAG="$(git -C "$REPO_ROOT" rev-parse --short=12 HEAD)"
  command -v dotnet >/dev/null || die "dotnet SDK required for --build-api"
  # The SDK pushes to a REMOTE registry only when ContainerRegistry is set; the
  # registry host must be split out of the repository name (folding it into
  # ContainerRepository silently loads to the local Docker daemon instead).
  REGISTRY_HOST="${REGISTRY%%/*}"                       # e.g. ghcr.io
  REGISTRY_PATH="${REGISTRY#"$REGISTRY_HOST"}"; REGISTRY_PATH="${REGISTRY_PATH#/}"  # e.g. fullstackhero
  API_REPO="${REGISTRY_PATH:+$REGISTRY_PATH/}$API_IMAGE_NAME"
  MIGRATOR_REPO="${REGISTRY_PATH:+$REGISTRY_PATH/}$MIGRATOR_IMAGE_NAME"
  echo "==> Building & pushing API image $REGISTRY_HOST/$API_REPO:$IMAGE_TAG"
  dotnet publish "$REPO_ROOT/src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj" \
    -c Release -r linux-x64 \
    /t:PublishContainer \
    -p:ContainerRegistry="$REGISTRY_HOST" \
    -p:ContainerRepository="$API_REPO" \
    -p:ContainerImageTags="$IMAGE_TAG"
  echo "==> Building & pushing migrator image $REGISTRY_HOST/$MIGRATOR_REPO:$IMAGE_TAG"
  dotnet publish "$REPO_ROOT/src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj" \
    -c Release -r linux-x64 \
    /t:PublishContainer \
    -p:ContainerRegistry="$REGISTRY_HOST" \
    -p:ContainerRepository="$MIGRATOR_REPO" \
    -p:ContainerImageTags="$IMAGE_TAG"
  TF_IMAGE_ARGS=(-var "container_registry=$REGISTRY" -var "api_image_name=$API_IMAGE_NAME" -var "migrator_image_name=$MIGRATOR_IMAGE_NAME" -var "container_image_tag=$IMAGE_TAG")
elif [[ -n "$IMAGE_TAG" ]]; then
  TF_IMAGE_ARGS=(-var "container_image_tag=$IMAGE_TAG")
fi

# ---- 2. terraform -----------------------------------------------------------
echo "==> terraform init"
terraform -chdir="$APP_STACK_DIR" init -reconfigure -input=false -backend-config="$ENV_DIR/backend.hcl"

APPLY_ARGS=(-var-file="$ENV_DIR/terraform.tfvars" "${TF_IMAGE_ARGS[@]}" -input=false)
[[ "$AUTO_APPROVE" == true ]] && APPLY_ARGS+=(-auto-approve)

echo "==> terraform apply"
terraform -chdir="$APP_STACK_DIR" apply "${APPLY_ARGS[@]}"

# ---- 2.5 db migrator (one-shot ECS task) ------------------------------------
run_ecs_task() {
  # $1 = label, $2 = optional --overrides JSON ("" for the task's baked command)
  local label="$1" overrides="${2:-}"
  local run_args task_arn exit_code reason
  echo "==> Running DbMigrator task ($label)"
  run_args=(--cluster "$MIG_CLUSTER" --task-definition "$MIG_TASKDEF" --launch-type FARGATE
    --network-configuration "awsvpcConfiguration={subnets=[$MIG_SUBNETS],securityGroups=[$MIG_SG],assignPublicIp=DISABLED}"
    --query 'tasks[0].taskArn' --output text)
  [[ -n "$overrides" ]] && run_args+=(--overrides "$overrides")
  task_arn="$(aws ecs run-task "${run_args[@]}")"
  [[ -n "$task_arn" && "$task_arn" != "None" ]] || die "run-task failed to start ($label)"
  echo "    task: $task_arn — waiting for it to stop..."
  aws ecs wait tasks-stopped --cluster "$MIG_CLUSTER" --tasks "$task_arn"
  exit_code="$(aws ecs describe-tasks --cluster "$MIG_CLUSTER" --tasks "$task_arn" --query 'tasks[0].containers[0].exitCode' --output text)"
  if [[ "$exit_code" != "0" ]]; then
    reason="$(aws ecs describe-tasks --cluster "$MIG_CLUSTER" --tasks "$task_arn" --query 'tasks[0].stoppedReason' --output text)"
    die "migrator ($label) exited ${exit_code} — ${reason} (logs: CloudWatch ${MIG_LOG_GROUP})"
  fi
  echo "    migrator ($label) succeeded."
}

if [[ "$SKIP_MIGRATE" == true ]]; then
  echo "==> Skipping DB migration (--skip-migrate)"
else
  MIG="$(terraform -chdir="$APP_STACK_DIR" output -json migrator 2>/dev/null || echo null)"
  if [[ "$MIG" == "null" || -z "$MIG" ]]; then
    echo "==> Migrator not provisioned (enable_migrator=false) — skipping"
  else
    MIG_CLUSTER="$(echo "$MIG" | jq -r .cluster_arn)"
    MIG_TASKDEF="$(echo "$MIG" | jq -r .task_definition_family)"
    MIG_CNAME="$(echo "$MIG" | jq -r .container_name)"
    MIG_SG="$(echo "$MIG" | jq -r .security_group_id)"
    MIG_SUBNETS="$(echo "$MIG" | jq -r '.subnet_ids | join(",")')"
    MIG_LOG_GROUP="$(echo "$MIG" | jq -r .log_group_name)"
    run_ecs_task "migrate" ""
    if [[ "$SEED_DEMO" == true ]]; then
      run_ecs_task "seed-demo" "{\"containerOverrides\":[{\"name\":\"${MIG_CNAME}\",\"command\":[\"seed-demo\"]}]}"
    fi
  fi
fi

# ---- 3. frontends -----------------------------------------------------------
if [[ "$SKIP_FRONTEND" == true ]]; then
  echo "==> Skipping frontend publish (--skip-frontend)"
else
  publish_spa() {
    local output_name="$1" client_dir="$2"
    local site bucket dist_id
    site="$(terraform -chdir="$APP_STACK_DIR" output -json "$output_name")"
    if [[ "$site" == "null" || -z "$site" ]]; then
      echo "==> $output_name not provisioned — skipping"
      return
    fi
    bucket="$(echo "$site" | jq -r .bucket_name)"
    dist_id="$(echo "$site" | jq -r .cloudfront_distribution_id)"
    echo "==> Building $client_dir"
    ( cd "$REPO_ROOT/clients/$client_dir" && npm ci && npm run build )
    echo "==> Publishing $client_dir -> s3://$bucket (config.json kept Terraform-managed)"
    aws s3 sync "$REPO_ROOT/clients/$client_dir/dist" "s3://$bucket" --delete --exclude config.json
    echo "==> Invalidating CloudFront $dist_id"
    aws cloudfront create-invalidation --distribution-id "$dist_id" --paths '/*' >/dev/null
  }

  publish_spa dashboard_site dashboard
  publish_spa admin_site admin
fi

echo
echo "==> Done. Endpoints:"
terraform -chdir="$APP_STACK_DIR" output api_url || true
terraform -chdir="$APP_STACK_DIR" output dashboard_site || true
terraform -chdir="$APP_STACK_DIR" output admin_site || true
