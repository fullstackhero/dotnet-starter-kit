using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Per-app prefix from the AppHost assembly name (FSH.Starter.AppHost -> fsh-starter); namespaces Docker volumes + resource names so multiple FSH apps don't clash.
#pragma warning disable CA1308 // resource + volume names are conventionally lowercase
var appPrefix = builder.Environment.ApplicationName
    .Replace(".AppHost", string.Empty, StringComparison.OrdinalIgnoreCase)
    .Replace('.', '-')
    .ToLowerInvariant();
#pragma warning restore CA1308

// Postgres + pgAdmin sidecar (auto-discovers registered databases); persistent so volumes and saved state survive restarts.
var postgresServer = builder.AddPostgres("postgres")
    .WithDataVolume($"{appPrefix}-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pa => pa
        .WithHostPort(5050)
        .WithLifetime(ContainerLifetime.Persistent));

var postgres = postgresServer.AddDatabase("fsh-db");

// Valkey (BSD-3 Redis fork) as a plain container: Aspire 13.4.0 AddRedis() forces TLS-by-default in run mode and never materializes the container, so we drop to plain RESP over TCP. Name stays "redis" so config keys don't churn.
var redis = builder.AddContainer("redis", "valkey/valkey", "9.1.0")
    .WithEndpoint(targetPort: 6379, scheme: "tcp", name: "tcp")
    .WithVolume($"{appPrefix}-redis-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

var redisEndpoint = redis.GetEndpoint("tcp");
var redisConnectionString = ReferenceExpression.Create(
    $"{redisEndpoint.Property(EndpointProperty.HostAndPort)}");

// RedisInsight cache browser (dev-only) sidecar; RI_REDIS_* pre-registers the Valkey connection via the container-network alias "redis".
builder.AddContainer("redis-insight", "redis/redisinsight", "latest")
    .WithHttpEndpoint(port: 5540, targetPort: 5540, name: "http")
    .WithEnvironment("RI_REDIS_HOST0", "redis")
    .WithEnvironment("RI_REDIS_PORT0", "6379")
    .WithEnvironment("RI_REDIS_ALIAS0", "fsh-cache")
    .WithEnvironment("RI_ACCEPT_TERMS_AND_CONDITIONS", "true")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(redis);

// Object storage (MinIO, S3-compatible). CORS via MINIO_API_CORS_ALLOW_ORIGIN so browser presigned PUTs from the admin (:5173)/dashboard (:5174) dev origins work without proxying through the API.
const string MinioBucket = "fsh-uploads";
const string AdminOrigin = "http://localhost:5173";
const string DashboardOrigin = "http://localhost:5174";

var minioUser = builder.AddParameter("minio-user", "minioadmin");
var minioPassword = builder.AddParameter("minio-password", "minioadmin", secret: true);

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithEnvironment("MINIO_ROOT_USER", minioUser)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioPassword)
    .WithEnvironment("MINIO_API_CORS_ALLOW_ORIGIN", $"{AdminOrigin},{DashboardOrigin}")
    .WithVolume($"{appPrefix}-minio-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

// Init container: bucket bootstrap (create + public-read). Script normalized to LF so /bin/sh in minio/mc doesn't choke on Windows CRLF.
var minioInitScript = ($$"""
until mc alias set local http://minio:9000 "$MC_USER" "$MC_PASS"; do
  echo "waiting for minio...";
  sleep 2;
done;
mc mb --ignore-existing local/{{MinioBucket}};
mc anonymous set download local/{{MinioBucket}};
""").ReplaceLineEndings("\n");

var minioInit = builder.AddContainer("minio-init", "minio/mc")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", minioInitScript)
    .WithEnvironment("MC_USER", minioUser)
    .WithEnvironment("MC_PASS", minioPassword)
    .WaitFor(minio);

var minioApiEndpoint = minio.GetEndpoint("api");

// DB migrator: applies pending migrations + seeds the root admin (admin@root.com), then exits; the API waits for its completion so it never starts against an unmigrated DB. Seed password is a dev-only default.
var migrator = builder.AddProject<Projects.FSH_Starter_DbMigrator>($"{appPrefix}-db-migrator")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Starter.Migrations.PostgreSQL")
    .WithEnvironment("Seed__DefaultAdminPassword", "123Pa$$word!")
    .WithArgs("apply", "--seed");

// Demo seeder (dev-only): provisions the acme/globex tenants + demo-login users via seed-demo. DOTNET_ENVIRONMENT=Development is required (console host ignores ASPNETCORE_ENVIRONMENT) or seed-demo refuses to run.
var demoSeeder = builder.AddProject<Projects.FSH_Starter_DbMigrator>($"{appPrefix}-demo-seeder")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WaitForCompletion(migrator)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Starter.Migrations.PostgreSQL")
    .WithEnvironment("Seed__DemoPassword", "Password123!")
    .WithArgs("seed-demo");

// API Service
var api = builder.AddProject<Projects.FSH_Starter_Api>($"{appPrefix}-api")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitForCompletion(minioInit)
    .WaitForCompletion(migrator)
    .WaitForCompletion(demoSeeder)
    .WithExternalHttpEndpoints()
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Starter.Migrations.PostgreSQL")
    .WithEnvironment("CachingOptions__Redis", redisConnectionString)
    .WithEnvironment("CachingOptions__EnableSsl", "false")
    // Hangfire dashboard (/jobs) creds — [Required], Password [MinLength(12)], ValidateOnStart; API won't boot without them. Dev-only, mirrors appsettings.Development.json.
    .WithEnvironment("HangfireOptions__UserName", "admin")
    .WithEnvironment("HangfireOptions__Password", "Password123!")
    .WithEnvironment("Storage__Provider", "s3")
    .WithEnvironment("Storage__S3__Bucket", MinioBucket)
    .WithEnvironment("Storage__S3__Region", "us-east-1")
    .WithEnvironment("Storage__S3__ServiceUrl", minioApiEndpoint)
    .WithEnvironment("Storage__S3__AccessKey", minioUser)
    .WithEnvironment("Storage__S3__SecretKey", minioPassword)
    .WithEnvironment("Storage__S3__ForcePathStyle", "true")
    .WithEnvironment("Storage__S3__PublicBaseUrl", ReferenceExpression.Create($"{minioApiEndpoint}/{MinioBucket}"));

//#if (frontend)
// Admin console (React + Vite). Target the API's HTTPS endpoint directly — UseHttpsRedirection's 307 to https is cross-origin and strips the Authorization header.
builder.AddJavaScriptApp($"{appPrefix}-admin", "../../../clients/admin", "dev")
    .WithNpm()
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5173, targetPort: 5173, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"));

// Tenant-facing dashboard (React + Vite, with SSE live feed)
builder.AddJavaScriptApp($"{appPrefix}-dashboard", "../../../clients/dashboard", "dev")
    .WithNpm()
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5174, targetPort: 5174, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"));
//#else
// React apps excluded: discard the unused api handle to keep the no-frontend scaffold warning-clean (S1481 under TreatWarningsAsErrors).
_ = api;
//#endif

await builder.Build().RunAsync();
