<#
.SYNOPSIS
  One-command deploy of the FullStackHero Starter Kit to AWS.

.EXAMPLE
  ./deploy.ps1 -Environment dev -Region us-east-1
  ./deploy.ps1 -Environment prod -Region ap-south-1 -BuildApi -AutoApprove

.DESCRIPTION
  Runs, in order:
    1. terraform init + apply (VPC, ALB+WAF, ECS API, RDS, Redis, S3, the two SPA CloudFront sites)
    2. (optional) build & push the API + DbMigrator container images
    3. run the DbMigrator one-shot ECS task (apply / apply --seed) and wait for exit 0
    4. build the React apps, publish to their S3 buckets, invalidate CloudFront

  Requires: terraform >= 1.15.4, aws cli (configured), node/npm, and
  (with -BuildApi) the .NET SDK + a registry login.
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][ValidateSet('dev', 'staging', 'prod')][string]$Environment,
  # Region is mandatory — never assume one. PowerShell prompts when it is omitted;
  # CI/automation passes -Region explicitly.
  [Parameter(Mandatory, HelpMessage = 'AWS region, e.g. us-east-1 or ap-south-1')][string]$Region,
  [switch]$BuildApi,
  [string]$ImageTag,
  [string]$Registry = 'ghcr.io/fullstackhero',
  [switch]$SkipMigrate,
  [switch]$SeedDemo,
  [switch]$SkipFrontend,
  [switch]$AutoApprove
)

$ErrorActionPreference = 'Stop'
$MinTfVersion = [version]'1.15.4'
$ApiImageName = 'fsh-api'
$MigratorImageName = 'fsh-db-migrator'

$ScriptDir = $PSScriptRoot
$RepoRoot = (Resolve-Path "$ScriptDir/../../../..").Path
$AppStackDir = Join-Path $ScriptDir 'app_stack'
$EnvDir = Join-Path $ScriptDir "envs/$Environment/$Region"

# Each env/region gets its OWN Terraform data dir (backend pointer, providers,
# modules) instead of the shared app_stack/.terraform. Without this, two runs
# against different backends — e.g. a deploy in one region while another region
# is being destroyed — clobber each other's backend pointer mid-run, so the
# later `terraform output` reads the wrong state ("Output not found").
$env:TF_DATA_DIR = Join-Path $AppStackDir ".terraform/$Environment-$Region"

function Die($msg) { Write-Error $msg; exit 1 }

if (-not (Test-Path "$EnvDir/backend.hcl")) { Die "no backend.hcl for $Environment/$Region at $EnvDir" }
if (-not (Test-Path "$EnvDir/terraform.tfvars")) { Die "no terraform.tfvars for $Environment/$Region at $EnvDir" }

# Note: no jq — PowerShell parses Terraform's JSON output with ConvertFrom-Json.
foreach ($tool in 'terraform', 'aws') {
  if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { Die "$tool is required but not installed" }
}

# Pin the region for every `aws` CLI call (else it falls back to ~/.aws/config →
# "Invalid Region in ARN" when that default differs from $Region).
$env:AWS_REGION = $Region
$env:AWS_DEFAULT_REGION = $Region

$tfVersion = [version](terraform version -json | ConvertFrom-Json).terraform_version
if ($tfVersion -lt $MinTfVersion) {
  Die "Terraform >= $MinTfVersion required (found $tfVersion). Upgrade with e.g. 'choco upgrade terraform'."
}

Write-Host "==> Deploying '$Environment' in $Region"

# ---- 1. optional API image build/push --------------------------------------
$tfImageArgs = @()
if ($BuildApi) {
  if (-not $ImageTag) { $ImageTag = (git -C $RepoRoot rev-parse --short=12 HEAD).Trim() }
  if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Die 'dotnet SDK required for -BuildApi' }
  # The SDK pushes to a REMOTE registry only when ContainerRegistry is set; the
  # registry host must be split out of the repository name (folding it into
  # ContainerRepository silently loads to the local Docker daemon instead).
  $registryHost = $Registry.Split('/')[0]                                   # e.g. ghcr.io
  $registryPath = $Registry.Substring($registryHost.Length).TrimStart('/')  # e.g. fullstackhero
  $apiRepo = if ($registryPath) { "$registryPath/$ApiImageName" } else { $ApiImageName }
  $migratorRepo = if ($registryPath) { "$registryPath/$MigratorImageName" } else { $MigratorImageName }
  Write-Host "==> Building & pushing API image $registryHost/$apiRepo`:$ImageTag"
  dotnet publish "$RepoRoot/src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj" `
    -c Release -r linux-x64 `
    /t:PublishContainer `
    -p:ContainerRegistry="$registryHost" `
    -p:ContainerRepository="$apiRepo" `
    -p:ContainerImageTags="$ImageTag"
  Write-Host "==> Building & pushing migrator image $registryHost/$migratorRepo`:$ImageTag"
  dotnet publish "$RepoRoot/src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj" `
    -c Release -r linux-x64 `
    /t:PublishContainer `
    -p:ContainerRegistry="$registryHost" `
    -p:ContainerRepository="$migratorRepo" `
    -p:ContainerImageTags="$ImageTag"
  $tfImageArgs = @('-var', "container_registry=$Registry", '-var', "api_image_name=$ApiImageName", '-var', "migrator_image_name=$MigratorImageName", '-var', "container_image_tag=$ImageTag")
}
elseif ($ImageTag) {
  $tfImageArgs = @('-var', "container_image_tag=$ImageTag")
}

# ---- 2. terraform -----------------------------------------------------------
# Native exes don't trip $ErrorActionPreference, so check $LASTEXITCODE
# explicitly — otherwise a failed apply silently falls through to the
# migrator/frontend steps (which then find no state and emit confusing errors).
Write-Host '==> terraform init'
terraform -chdir="$AppStackDir" init -reconfigure -input=false -backend-config="$EnvDir/backend.hcl"
if ($LASTEXITCODE -ne 0) { Die "terraform init failed (exit $LASTEXITCODE)" }

$applyArgs = @("-var-file=$EnvDir/terraform.tfvars") + $tfImageArgs + @('-input=false')
if ($AutoApprove) { $applyArgs += '-auto-approve' }

Write-Host '==> terraform apply'
terraform -chdir="$AppStackDir" apply @applyArgs
if ($LASTEXITCODE -ne 0) { Die "terraform apply failed (exit $LASTEXITCODE)" }

# ---- 2.5 db migrator (one-shot ECS task) ------------------------------------
function Invoke-EcsTask($label, $overridesJson) {
  Write-Host "==> Running DbMigrator task ($label)"
  $runArgs = @(
    'ecs', 'run-task',
    '--cluster', $migCluster,
    '--task-definition', $migTaskDef,
    '--launch-type', 'FARGATE',
    '--network-configuration', "awsvpcConfiguration={subnets=[$migSubnets],securityGroups=[$migSg],assignPublicIp=DISABLED}",
    '--query', 'tasks[0].taskArn', '--output', 'text'
  )
  if ($overridesJson) {
    $tmp = New-TemporaryFile
    Set-Content -Path $tmp.FullName -Value $overridesJson -Encoding utf8
    $runArgs += @('--overrides', "file://$($tmp.FullName)")
  }
  $taskArn = (aws @runArgs)
  if ($LASTEXITCODE -ne 0 -or -not $taskArn) { Die "run-task failed to start ($label) — see aws error above (region: $env:AWS_REGION)" }
  $taskArn = "$taskArn".Trim()
  if (-not $taskArn -or $taskArn -eq 'None') { Die "run-task failed to start ($label)" }
  Write-Host "    task: $taskArn — waiting for it to stop..."
  aws ecs wait tasks-stopped --cluster $migCluster --tasks $taskArn
  $exitCode = (aws ecs describe-tasks --cluster $migCluster --tasks $taskArn --query 'tasks[0].containers[0].exitCode' --output text).Trim()
  if ($exitCode -ne '0') {
    $reason = (aws ecs describe-tasks --cluster $migCluster --tasks $taskArn --query 'tasks[0].stoppedReason' --output text).Trim()
    Die "migrator ($label) exited $exitCode — $reason (logs: CloudWatch $migLogGroup)"
  }
  Write-Host "    migrator ($label) succeeded."
}

if ($SkipMigrate) {
  Write-Host '==> Skipping DB migration (-SkipMigrate)'
}
else {
  $migJson = terraform -chdir="$AppStackDir" output -json migrator 2>$null
  if (-not $migJson -or $migJson -eq 'null') {
    Write-Host '==> Migrator not provisioned (enable_migrator=false) — skipping'
  }
  else {
    $mig = $migJson | ConvertFrom-Json
    $migCluster = $mig.cluster_arn
    $migTaskDef = $mig.task_definition_family
    $migCName = $mig.container_name
    $migSg = $mig.security_group_id
    $migSubnets = ($mig.subnet_ids -join ',')
    $migLogGroup = $mig.log_group_name
    Invoke-EcsTask 'migrate' $null
    if ($SeedDemo) {
      $ov = "{`"containerOverrides`":[{`"name`":`"$migCName`",`"command`":[`"seed-demo`"]}]}"
      Invoke-EcsTask 'seed-demo' $ov
    }
  }
}

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
