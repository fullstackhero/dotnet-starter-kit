$hostDirectory = 'D:\Projects\fullstackhero\dotnet-webapi-boilerplate\src\Host'
Set-Location -Path $hostDirectory

$commitMessage = 'UpdateREADME'

<# Declaring Connection String #>
$mssqlConnectionString = "Data Source=(localdb)\mssqllocaldb;Initial Catalog=rootTenantDb;Integrated Security=True;MultipleActiveResultSets=True"
$postgresqlConnectionString = "Host=localhost;Database=rootTenantDb;Username=postgres;Password=root;Include Error Detail=true"
$mysqlConnectionString = "server=localhost;uid=root;pwd=root;database=defaultRootDb;Allow User Variables=True"

<# Defining JSON Paths #>
$databaseJsonPath ='Configurations\database.json'
$hangfireJsonPath = 'Configurations\hangfire.json'

<# Get Current Config #>

$databaseJsonContent = Get-Content $databaseJsonPath -raw | ConvertFrom-Json
$currentDbProvider = $databaseJsonContent.DatabaseSettings.DBProvider
$currentConnectionString = $databaseJsonContent.DatabaseSettings.ConnectionString

$hangfireJsonContent = Get-Content $hangfireJsonPath -raw | ConvertFrom-Json
$currentConnectionStringForHangfire = $hangfireJsonContent.HangfireSettings.Storage.ConnectionString
$currentDbProviderForHangfire = $hangfireJsonContent.HangfireSettings.Storage.StorageProvider

<# MSSQL #>
$databaseJsonContent.DatabaseSettings.DBProvider = "mssql"
$databaseJsonContent.DatabaseSettings.ConnectionString = $mssqlConnectionString
$databaseJsonContent | ConvertTo-Json -Depth 4  | set-content $databaseJsonPath

$hangfireJsonContent.HangfireSettings.Storage.StorageProvider = "mssql"
$hangfireJsonContent.HangfireSettings.Storage.ConnectionString = $mssqlConnectionString
$hangfireJsonContent | ConvertTo-Json -Depth 4  | set-content $hangfireJsonPath

dotnet ef migrations add $commitMessage --project .././Migrators/Migrators.MSSQL/ --context ApplicationDbContext -o Migrations/Application

<# MySQL #>
$databaseJsonContent.DatabaseSettings.DBProvider = "mysql"
$databaseJsonContent.DatabaseSettings.ConnectionString = $mysqlConnectionString
$databaseJsonContent | ConvertTo-Json -Depth 4  | set-content $databaseJsonPath

$hangfireJsonContent.HangfireSettings.Storage.StorageProvider = "mysql"
$hangfireJsonContent.HangfireSettings.Storage.ConnectionString = $mysqlConnectionString
$hangfireJsonContent | ConvertTo-Json -Depth 4  | set-content $hangfireJsonPath

dotnet ef migrations add $commitMessage --project .././Migrators/Migrators.MySQL/ --context ApplicationDbContext -o Migrations/Application

<# PostgreSQL #>
$databaseJsonContent.DatabaseSettings.DBProvider = "postgresql"
$databaseJsonContent.DatabaseSettings.ConnectionString = $postgresqlConnectionString
$databaseJsonContent | ConvertTo-Json -Depth 4  | set-content $databaseJsonPath

$hangfireJsonContent.HangfireSettings.Storage.StorageProvider = "postgresql"
$hangfireJsonContent.HangfireSettings.Storage.ConnectionString = $postgresqlConnectionString
$hangfireJsonContent | ConvertTo-Json -Depth 4  | set-content $hangfireJsonPath

dotnet ef migrations add $commitMessage --project .././Migrators/Migrators.PostgreSQL/ --context ApplicationDbContext -o Migrations/Application

<# Reset Configurations - Switch Back to Original Configurations #>

$databaseJsonContent.DatabaseSettings.DBProvider = $currentDbProvider
$databaseJsonContent.DatabaseSettings.ConnectionString = $currentConnectionString
$databaseJsonContent | ConvertTo-Json -Depth 4  | set-content $databaseJsonPath

$hangfireJsonContent.HangfireSettings.Storage.StorageProvider = $currentDbProviderForHangfire
$hangfireJsonContent.HangfireSettings.Storage.ConnectionString = $currentConnectionStringForHangfire
$hangfireJsonContent | ConvertTo-Json -Depth 4  | set-content $hangfireJsonPath