<#
.SYNOPSIS
  One-command deploy of the FullStackHero Starter Kit to AWS.

.EXAMPLE
  ./deploy.ps1 -Environment dev
  ./deploy.ps1 -Environment prod -Region us-east-1 -BuildApi -AutoApprove

.DESCRIPTION
  Runs, in order:
    1. terraform init + apply (VPC, ALB+WAF, ECS API, RDS, Redis, S3, the two SPA CloudFront sites)
    2. (optional) build & push the API container image
    3. build the React apps, publish to their S3 buckets, invalidate CloudFront

  Requires: terraform >= 1.15.4, aws cli (configured), jq, node/npm, and
  (with -BuildApi) the .NET SDK + a registry login.
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][ValidateSet('dev', 'staging', 'prod')][string]$Environment,
  [string]$Region = 'us-east-1',
  [switch]$BuildApi,
  [string]$ImageTag,
  [string]$Registry = 'ghcr.io/fullstackhero',
  [switch]$SkipFrontend,
  [switch]$AutoApprove
)

$ErrorActionPreference = 'Stop'
$MinTfVersion = [version]'1.15.4'
$ApiImageName = 'fsh-api'

$ScriptDir = $PSScriptRoot
$RepoRoot = (Resolve-Path "$ScriptDir/../../../..").Path
$AppStackDir = Join-Path $ScriptDir 'app_stack'
$EnvDir = Join-Path $ScriptDir "envs/$Environment/$Region"

function Die($msg) { Write-Error $msg; exit 1 }

if (-not (Test-Path "$EnvDir/backend.hcl")) { Die "no backend.hcl for $Environment/$Region at $EnvDir" }
if (-not (Test-Path "$EnvDir/terraform.tfvars")) { Die "no terraform.tfvars for $Environment/$Region at $EnvDir" }

foreach ($tool in 'terraform', 'aws', 'jq') {
  if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { Die "$tool is required but not installed" }
}

$tfVersion = [version](terraform version -json | ConvertFrom-Json).terraform_version
if ($tfVersion -lt $MinTfVersion) {
  Die "Terraform >= $MinTfVersion required (found $tfVersion). Upgrade with e.g. 'choco upgrade terraform'."
}

Write-Host "==> Deploying '$Environment' in $Region"

# ---- 1. optional API image build/push --------------------------------------
$tfImageArgs = @()
if ($BuildApi) {
  if (-not $ImageTag) { $ImageTag = (git -C $RepoRoot rev-parse --short=12 HEAD).Trim() }
  Write-Host "==> Building & pushing API image $Registry/$ApiImageName`:$ImageTag"
  if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Die 'dotnet SDK required for -BuildApi' }
  dotnet publish "$RepoRoot/src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj" `
    -c Release -r linux-x64 `
    /t:PublishContainer `
    -p:ContainerRepository="$Registry/$ApiImageName" `
    -p:ContainerImageTags="$ImageTag"
  $tfImageArgs = @('-var', "container_registry=$Registry", '-var', "api_image_name=$ApiImageName", '-var', "container_image_tag=$ImageTag")
}
elseif ($ImageTag) {
  $tfImageArgs = @('-var', "container_image_tag=$ImageTag")
}

# ---- 2. terraform -----------------------------------------------------------
Write-Host '==> terraform init'
terraform -chdir="$AppStackDir" init -reconfigure -input=false -backend-config="$EnvDir/backend.hcl"

$applyArgs = @("-var-file=$EnvDir/terraform.tfvars") + $tfImageArgs + @('-input=false')
if ($AutoApprove) { $applyArgs += '-auto-approve' }

Write-Host '==> terraform apply'
terraform -chdir="$AppStackDir" apply @applyArgs

# ---- 3. frontends -----------------------------------------------------------
function Publish-Spa($outputName, $clientDir) {
  $json = terraform -chdir="$AppStackDir" output -json $outputName
  if (-not $json -or $json -eq 'null') { Write-Host "==> $outputName not provisioned — skipping"; return }
  $site = $json | ConvertFrom-Json
  Write-Host "==> Building $clientDir"
  Push-Location "$RepoRoot/clients/$clientDir"
  try { npm ci; npm run build } finally { Pop-Location }
  Write-Host "==> Publishing $clientDir -> s3://$($site.bucket_name) (config.json kept Terraform-managed)"
  aws s3 sync "$RepoRoot/clients/$clientDir/dist" "s3://$($site.bucket_name)" --delete --exclude config.json
  Write-Host "==> Invalidating CloudFront $($site.cloudfront_distribution_id)"
  aws cloudfront create-invalidation --distribution-id $site.cloudfront_distribution_id --paths '/*' | Out-Null
}

if ($SkipFrontend) {
  Write-Host '==> Skipping frontend publish (-SkipFrontend)'
}
else {
  Publish-Spa 'dashboard_site' 'dashboard'
  Publish-Spa 'admin_site' 'admin'
}

Write-Host "`n==> Done. Endpoints:"
terraform -chdir="$AppStackDir" output api_url
terraform -chdir="$AppStackDir" output dashboard_site
terraform -chdir="$AppStackDir" output admin_site
