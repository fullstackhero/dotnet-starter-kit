param(
     [Parameter(Mandatory=$true)]
     [ValidateNotNullOrEmpty()]
     [string]$commitMessage  
 )
 
$rootDirectory = git rev-parse --show-toplevel
$hostDirectory = $rootDirectory + '/src/Host'
Set-Location -Path $hostDirectory
Write-Host "Host Directory is $hostDirectory `n"

<# $commitMessage = 'UpdateREADME' #>
Write-Host "Commit Message is $commitMessage `n"

<# Declaring Connection String #>
Write-Host "Setting Connection Strings..."
$mssqlConnectionString = "Data Source=(localdb)\mssqllocaldb;Initial Catalog=rootTenantDb;Integrated Security=True;MultipleActiveResultSets=True"
$postgresqlConnectionString = "Host=localhost;Database=rootTenantDb;Username=postgres;Password=root;Include Error Detail=true"
$mysqlConnectionString = "server=localhost;uid=root;pwd=root;database=defaultRootDb;Allow User Variables=True"

<# Defining JSON Paths #>
Write-Host "Defining JSON Paths... `n"
$databaseJsonPath = 'Configurations/database.json'
$hangfireJsonPath = 'Configurations/hangfire.json'

<# Get Current Config #>
Write-Host "Getting Current Config...`n"
$databaseFileContent = Get-Content $databaseJsonPath -raw
$hangfireFileContent = Get-Content $hangfireJsonPath -raw 

Write-Host "Creating Config Objects...`n"
$databaseJsonContent = Get-Content $databaseJsonPath -raw | ConvertFrom-Json
$hangfireJsonContent = Get-Content $hangfireJsonPath -raw | ConvertFrom-Json
Write-Host "**************************`n"

<# MSSQL #>
Write-Host "Updating Configurations for MSSQL Provider..."
$databaseJsonContent.DatabaseSettings.DBProvider = "mssql"
$databaseJsonContent.DatabaseSettings.ConnectionString = $mssqlConnectionString
$databaseJsonContent | ConvertTo-Json | set-content $databaseJsonPath

$hangfireJsonContent.HangfireSettings.Storage.StorageProvider = "mssql"
$hangfireJsonContent.HangfireSettings.Storage.ConnectionString = $mssqlConnectionString
$hangfireJsonContent | ConvertTo-Json | set-content $hangfireJsonPath

Write-Host "Adding Migrations for MSSQL Provider..."
dotnet ef migrations add $commitMessage --project .././Migrators/Migrators.MSSQL/ --context ApplicationDbContext -o Migrations/Application
Write-Host "Adding Migrations for MSSQL Provider...Done`n"
Write-Host "**************************`n"

<# MySQL #>
Write-Host "Updating Configurations for MySQL Provider..."
$databaseJsonContent.DatabaseSettings.DBProvider = "mysql"
$databaseJsonContent.DatabaseSettings.ConnectionString = $mysqlConnectionString
$databaseJsonContent | ConvertTo-Json | set-content $databaseJsonPath

$hangfireJsonContent.HangfireSettings.Storage.StorageProvider = "mysql"
$hangfireJsonContent.HangfireSettings.Storage.ConnectionString = $mysqlConnectionString
$hangfireJsonContent | ConvertTo-Json | set-content $hangfireJsonPath

Write-Host "Adding Migrations for MySQL Provider..."
dotnet ef migrations add $commitMessage --project .././Migrators/Migrators.MySQL/ --context ApplicationDbContext -o Migrations/Application
Write-Host "Adding Migrations for MySQL Provider...Done`n"
Write-Host "**************************`n"

<# PostgreSQL #>
Write-Host "Updating Configurations for PostgreSQL Provider..."
$databaseJsonContent.DatabaseSettings.DBProvider = "postgresql"
$databaseJsonContent.DatabaseSettings.ConnectionString = $postgresqlConnectionString
$databaseJsonContent | ConvertTo-Json | set-content $databaseJsonPath

$hangfireJsonContent.HangfireSettings.Storage.StorageProvider = "postgresql"
$hangfireJsonContent.HangfireSettings.Storage.ConnectionString = $postgresqlConnectionString
$hangfireJsonContent | ConvertTo-Json | set-content $hangfireJsonPath

Write-Host "Adding Migrations for PostgreSQL Provider..."
dotnet ef migrations add $commitMessage --project .././Migrators/Migrators.PostgreSQL/ --context ApplicationDbContext -o Migrations/Application
Write-Host "Adding Migrations for PostgreSQL Provider...Done`n"
Write-Host "**************************`n"

<# Reset Configurations - Switch Back to Original Configurations #>
Write-Host "Resetting Configurations to Orginal...`n"
$databaseFileContent | set-content -NoNewline -Force $databaseJsonPath
$hangfireFileContent | set-content -NoNewline -Force $hangfireJsonPath
Write-Host "**************************`n"

Set-Location -Path $currentDirectory
Write-Host -NoNewLine 'Migrations Added. Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
