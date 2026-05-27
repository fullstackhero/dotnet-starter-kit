#!/usr/bin/env bash
#
# One-command deploy of the FullStackHero Starter Kit to AWS.
#
#   ./deploy.sh <dev|staging|prod> [region]
#
# It will, in order:
#   1. terraform init + apply (infra: VPC, ALB+WAF, ECS API, RDS, Redis, S3, the two SPA CloudFront sites)
#   2. (optional) build & push the API container image
#   3. build the React apps and publish them to their S3 buckets + invalidate CloudFront
#
# Flags:
#   --build-api          Build & push the API image at the current git SHA and deploy that tag.
#   --image-tag TAG      Deploy a specific, already-published API image tag.
#   --registry REG       Container registry (default: ghcr.io/fullstackhero). Used only with --build-api.
#   --skip-frontend      Skip building/publishing the SPAs.
#   --auto-approve       Don't prompt before applying.
#
# Requires: terraform >= 1.15.4, aws cli (configured creds), jq, node/npm, and
# (with --build-api) the .NET SDK + a registry login.
set -euo pipefail

MIN_TF_VERSION="1.15.4"
DEFAULT_REGISTRY="ghcr.io/fullstackhero"
API_IMAGE_NAME="fsh-api"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"
APP_STACK_DIR="$SCRIPT_DIR/app_stack"

# ---- args -------------------------------------------------------------------
ENVIRONMENT="${1:-}"
REGION="us-east-1"
BUILD_API=false
SKIP_FRONTEND=false
AUTO_APPROVE=false
IMAGE_TAG=""
REGISTRY="$DEFAULT_REGISTRY"

shift || true
while [[ $# -gt 0 ]]; do
  case "$1" in
    --build-api) BUILD_API=true ;;
    --skip-frontend) SKIP_FRONTEND=true ;;
    --auto-approve) AUTO_APPROVE=true ;;
    --image-tag) IMAGE_TAG="${2:-}"; shift ;;
    --registry) REGISTRY="${2:-}"; shift ;;
    dev|staging|prod) ENVIRONMENT="$1" ;;
    *) if [[ "$1" =~ ^[a-z]{2}-[a-z]+-[0-9]$ ]]; then REGION="$1"; else echo "Unknown argument: $1" >&2; exit 2; fi ;;
  esac
  shift
done

die() { echo "ERROR: $*" >&2; exit 1; }

[[ "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]] || die "usage: ./deploy.sh <dev|staging|prod> [region] [flags]"

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
  echo "==> Building & pushing API image $REGISTRY/$API_IMAGE_NAME:$IMAGE_TAG"
  command -v dotnet >/dev/null || die "dotnet SDK required for --build-api"
  dotnet publish "$REPO_ROOT/src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj" \
    -c Release -r linux-x64 \
    /t:PublishContainer \
    -p:ContainerRepository="$REGISTRY/$API_IMAGE_NAME" \
    -p:ContainerImageTags="$IMAGE_TAG"
  TF_IMAGE_ARGS=(-var "container_registry=$REGISTRY" -var "api_image_name=$API_IMAGE_NAME" -var "container_image_tag=$IMAGE_TAG")
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
