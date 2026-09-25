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

// Warm pooled-connection floor for the long-running API — Npgsql's default Minimum Pool Size of 0 lets the pool drain to cold, so /health/ready's ~10 concurrent DbContext checks cold-open a cohort at once and intermittently stall the probe; a floor keeps connections warm for reuse.
var apiPgConnection = ReferenceExpression.Create(
    $"{postgres.Resource.ConnectionStringExpression};Minimum Pool Size=5");

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

// Object storage (RustFS, S3-compatible; replaces MinIO, whose images were withdrawn). CORS via RUSTFS_CORS_ALLOWED_ORIGINS so browser presigned PUTs from the admin (:5173)/dashboard (:5174) dev origins work without proxying through the API.
const string S3Bucket = "fsh-uploads";
const string AdminOrigin = "http://localhost:5173";
const string DashboardOrigin = "http://localhost:5174";

var s3User = builder.AddParameter("rustfs-user", "rustfsadmin");
var s3Password = builder.AddParameter("rustfs-password", "rustfsadmin", secret: true);

var rustfs = builder.AddContainer("rustfs", "rustfs/rustfs", "1.0.0")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithEnvironment("RUSTFS_ACCESS_KEY", s3User)
    .WithEnvironment("RUSTFS_SECRET_KEY", s3Password)
    .WithEnvironment("RUSTFS_CONSOLE_ENABLE", "true")
    .WithEnvironment("RUSTFS_CORS_ALLOWED_ORIGINS", $"{AdminOrigin},{DashboardOrigin}")
    .WithVolume($"{appPrefix}-rustfs-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

// Init container: bucket bootstrap (create + public-read GetObject policy). Script normalized to LF so /bin/sh in aws-cli doesn't choke on Windows CRLF.
var s3InitScript = ($$"""
until aws --endpoint-url http://rustfs:9000 s3api list-buckets > /dev/null 2>&1; do
  echo "waiting for rustfs...";
  sleep 2;
done;
aws --endpoint-url http://rustfs:9000 s3api head-bucket --bucket {{S3Bucket}} 2>/dev/null || aws --endpoint-url http://rustfs:9000 s3api create-bucket --bucket {{S3Bucket}};
aws --endpoint-url http://rustfs:9000 s3api put-bucket-policy --bucket {{S3Bucket}} --policy '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{S3Bucket}}/*"]}]}';
""").ReplaceLineEndings("\n");

var s3Init = builder.AddContainer("rustfs-init", "amazon/aws-cli", "2.37.3")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", s3InitScript)
    .WithEnvironment("AWS_ACCESS_KEY_ID", s3User)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", s3Password)
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WaitFor(rustfs);

var s3ApiEndpoint = rustfs.GetEndpoint("api");

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
    .WaitForCompletion(s3Init)
    .WaitForCompletion(migrator)
    .WaitForCompletion(demoSeeder)
    .WithExternalHttpEndpoints()
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", apiPgConnection)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Starter.Migrations.PostgreSQL")
    .WithEnvironment("CachingOptions__Redis", redisConnectionString)
    .WithEnvironment("CachingOptions__EnableSsl", "false")
    // Hangfire dashboard (/jobs) creds — [Required], Password [MinLength(12)], ValidateOnStart; API won't boot without them. Dev-only, mirrors appsettings.Development.json.
    .WithEnvironment("HangfireOptions__UserName", "admin")
    .WithEnvironment("HangfireOptions__Password", "Password123!")
    // SMTP via Ethereal (https://ethereal.email) — fake catch-all inbox for local dev (nothing delivered); mirrors appsettings.Development.json. Safe to commit: throwaway test creds.
    .WithEnvironment("MailOptions__UseSendGrid", "false")
    .WithEnvironment("MailOptions__From", "nicole.lueilwitz0@ethereal.email")
    .WithEnvironment("MailOptions__DisplayName", "Mukesh Murugan")
    .WithEnvironment("MailOptions__Smtp__Host", "smtp.ethereal.email")
    .WithEnvironment("MailOptions__Smtp__Port", "587")
    .WithEnvironment("MailOptions__Smtp__UserName", "nicole.lueilwitz0@ethereal.email")
    .WithEnvironment("MailOptions__Smtp__Password", "x4VJz2r9x2NDss9KpC")
    .WithEnvironment("Storage__Provider", "s3")
    .WithEnvironment("Storage__S3__Bucket", S3Bucket)
    .WithEnvironment("Storage__S3__Region", "us-east-1")
    .WithEnvironment("Storage__S3__ServiceUrl", s3ApiEndpoint)
    .WithEnvironment("Storage__S3__AccessKey", s3User)
    .WithEnvironment("Storage__S3__SecretKey", s3Password)
    .WithEnvironment("Storage__S3__ForcePathStyle", "true")
    .WithEnvironment("Storage__S3__PublicBaseUrl", ReferenceExpression.Create($"{s3ApiEndpoint}/{S3Bucket}"));

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
