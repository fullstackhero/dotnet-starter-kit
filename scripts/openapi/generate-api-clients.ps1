param(
    [string]$SpecUrl = "https://localhost:7030/openapi/v1.json"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Get-Item (Join-Path $scriptDir "..\..")).FullName
$configPath = Join-Path $scriptDir "nswag-playground.json"
$outputDir = Join-Path $repoRoot "src\Playground\Playground.Blazor\ApiClient"

Write-Host "Ensuring dotnet local tools are restored..." -ForegroundColor Cyan
dotnet tool restore | Out-Host

Write-Host "Ensuring output directory exists: $outputDir" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Write-Host "Generating API client from spec: $SpecUrl" -ForegroundColor Cyan
dotnet nswag run $configPath /variables:SpecUrl=$SpecUrl

Write-Host "Done. Generated clients are in $outputDir" -ForegroundColor Green
