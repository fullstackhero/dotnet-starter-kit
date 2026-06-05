<#
.SYNOPSIS
  Destroy the FullStackHero Starter Kit AWS stack for one environment.

.EXAMPLE
  ./destroy.ps1 -Environment dev -Region us-east-1
  ./destroy.ps1 -Environment dev -Region us-east-1 -AutoApprove

.DESCRIPTION
  Runs terraform init (re-pointing the backend) + terraform destroy for the
  given env/region. Tears down the app_stack (VPC, ALB+WAF, ECS, RDS, Redis,
  S3 + CloudFront SPAs, secrets, IAM). Does NOT remove the Terraform state
  bucket/lock (separate bootstrap stack) or the GHCR container images.

  dev S3 buckets are force_destroy and RDS skips the final snapshot, so dev
  tears down cleanly. staging/prod enable RDS + ALB deletion protection, so
  destroy will fail until those are disabled — by design.

  Requires: terraform >= 1.15.4, aws cli (configured).
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][ValidateSet('dev', 'staging', 'prod')][string]$Environment,
  # Region is mandatory — never assume one. PowerShell prompts when it is omitted.
  [Parameter(Mandatory, HelpMessage = 'AWS region, e.g. us-east-1 or ap-south-1')][string]$Region,
  [switch]$SkipInit,
  [switch]$AutoApprove
)

$ErrorActionPreference = 'Stop'
$MinTfVersion = [version]'1.15.4'

$ScriptDir = $PSScriptRoot
$AppStackDir = Join-Path $ScriptDir 'app_stack'
$EnvDir = Join-Path $ScriptDir "envs/$Environment/$Region"

# Per-env/region Terraform data dir so a destroy never clobbers (or is clobbered
# by) a concurrent deploy/destroy in another region sharing app_stack/.terraform.
$env:TF_DATA_DIR = Join-Path $AppStackDir ".terraform/$Environment-$Region"

function Die($msg) { Write-Error $msg; exit 1 }

if (-not (Test-Path "$EnvDir/backend.hcl")) { Die "no backend.hcl for $Environment/$Region at $EnvDir" }
if (-not (Test-Path "$EnvDir/terraform.tfvars")) { Die "no terraform.tfvars for $Environment/$Region at $EnvDir" }

foreach ($tool in 'terraform', 'aws') {
  if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { Die "$tool is required but not installed" }
}

# Pin the region for every `aws` CLI call (else it falls back to ~/.aws/config).
$env:AWS_REGION = $Region
$env:AWS_DEFAULT_REGION = $Region

$tfVersion = [version](terraform version -json | ConvertFrom-Json).terraform_version
if ($tfVersion -lt $MinTfVersion) {
  Die "Terraform >= $MinTfVersion required (found $tfVersion)."
}

# Typed confirmation guard — irreversible, and mandatory for non-dev.
if (-not $AutoApprove) {
  Write-Host "==> About to DESTROY the '$Environment' stack in $Region. This is irreversible." -ForegroundColor Yellow
  $answer = Read-Host "    Type the environment name ('$Environment') to confirm"
  if ($answer -ne $Environment) { Die 'aborted (confirmation did not match)' }
}

# Native exes don't trip $ErrorActionPreference, so check $LASTEXITCODE explicitly.
if (-not $SkipInit) {
  Write-Host '==> terraform init'
  terraform -chdir="$AppStackDir" init -reconfigure -input=false -backend-config="$EnvDir/backend.hcl"
  if ($LASTEXITCODE -ne 0) { Die "terraform init failed (exit $LASTEXITCODE)" }
}

$destroyArgs = @("-var-file=$EnvDir/terraform.tfvars", '-input=false')
if ($AutoApprove) { $destroyArgs += '-auto-approve' }

Write-Host "==> terraform destroy '$Environment' ($Region) — CloudFront deletions can take ~15-20 min"
terraform -chdir="$AppStackDir" destroy @destroyArgs
if ($LASTEXITCODE -ne 0) { Die "terraform destroy failed (exit $LASTEXITCODE)" }

Write-Host "`n==> Destroyed '$Environment' in $Region. State bucket/lock and GHCR images are untouched."
