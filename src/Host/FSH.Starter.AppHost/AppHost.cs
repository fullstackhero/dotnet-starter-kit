using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Per-app prefix for Docker volume names, derived from the AppHost assembly name,
// so two FSH-based apps (or this repo + a scaffolded app) on one machine get
// isolated data volumes instead of clashing on shared "postgres-data"/etc. names.
#pragma warning disable CA1308 // Docker volume names are conventionally lowercase
var volumePrefix = builder.Environment.ApplicationName
    .Replace(".AppHost", string.Empty, StringComparison.OrdinalIgnoreCase)
    .Replace('.', '-')
    .ToLowerInvariant();
#pragma warning restore CA1308

// Infrastructure
//
// pgAdmin sidecar attaches to the same Postgres server resource. Aspire
// auto-discovers every database registered against this server, so the
// pgAdmin server-tree shows `fsh-db` (and any future databases) without
// us authoring a `servers.json` by hand. Persistent so the saved query
// state and folder layout survive `dotnet run` restarts.
var postgresServer = builder.AddPostgres("postgres")
    .WithDataVolume($"{volumePrefix}-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pa => pa
        .WithHostPort(5050)
        .WithLifetime(ContainerLifetime.Persistent));

var postgres = postgresServer.AddDatabase("fsh-db");

// Valkey (BSD-3, the Linux Foundation's Redis fork) via Aspire's Redis
// integration. Drop-in over RESP: the kit uses only StackExchange.Redis
// (cache, data protection, SignalR backplane, quota) with no Redis Stack
// modules, and Hangfire is on Postgres — so nothing is Redis-specific.
// Resource name stays "redis" so connection strings / config keys don't churn.
var redis = builder.AddRedis("redis")
    .WithImage("valkey/valkey", "8")
    .WithDataVolume($"{volumePrefix}-redis-data")
    .WithLifetime(ContainerLifetime.Persistent)
    // RedisInsight cache browser — Aspire auto-wires it to the resource above,
    // so it connects to Valkey with no manual config. It's SSPL (source-available),
    // but as a dev-only tool that's never redistributed the licensing impact is nil.
    // Prefer a strictly-MIT toolchain? Swap to .WithRedisCommander().
    .WithRedisInsight();

// Build a plain TCP (non-TLS) Redis connection string using the secondary endpoint.
// Aspire 13.x enables TLS on the primary Redis port by default; the secondary endpoint is plain TCP.
var redisPlainTcp = redis.GetEndpoint("secondary");
var redisConnectionString = ReferenceExpression.Create(
    $"{redisPlainTcp.Property(EndpointProperty.HostAndPort)},password={redis.Resource.PasswordParameter!}");

// Object storage (MinIO, S3-compatible)
//
// CORS: we configure MinIO to accept browser PUTs from the admin (:5173) and
// dashboard (:5174) dev origins so the Files module's presigned-URL upload
// flow works end-to-end without proxying bytes through the API. Modern MinIO
// (RELEASE.2024-*+) exposes this via the MINIO_API_CORS_ALLOW_ORIGIN server
// env var rather than `mc admin config set cors_*` — the old subsystem names
// were removed, and configuring it at server start also avoids the
// `mc admin service restart` TTY-not-available error inside the init container.
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
    .WithVolume($"{volumePrefix}-minio-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

// Init container: just bucket bootstrap (creation + public-read policy). CORS
// is handled by the env var above, so no `mc admin config set` / `service
// restart` is needed.
//
// Normalize line endings to LF — on Windows the source file is CRLF, and
// /bin/sh inside the minio/mc container chokes on \r appearing after `do`
// and `done` ("syntax error near unexpected token `done'").
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

// Database migrator — runs once on each AppHost launch, applies pending
// migrations across the tenant catalog + every tenant's per-module databases,
// then seeds the root tenant's admin user, then exits. The API depends on its
// completion below, so the API never starts against an unmigrated/unseeded
// database. Production deployments use this same project as an explicit step.
//
// `--seed` runs SeedTenantAsync so the root admin (admin@root.com) exists out of
// the box — without it the app comes up with an empty Users table and nobody can
// log in. The seed password is a dev-only default that mirrors the API's
// appsettings.Development.json; override it for non-dev use.
var migrator = builder.AddProject<Projects.FSH_Starter_DbMigrator>("fsh-db-migrator")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__ConnectionString", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Starter.Migrations.PostgreSQL")
    .WithEnvironment("Seed__DefaultAdminPassword", "123Pa$$word!")
    .WithArgs("apply", "--seed");

// Demo data seeder (dev-only) — provisions the `acme` and `globex` demo
// tenants plus the users the dashboard's demo-login panel advertises. The base
// migrator above runs `apply --seed`, which only creates the root tenant +
// root admin; the demo tenants are created exclusively by the migrator's
// `seed-demo` verb. Without this step those acme/globex logins fail against a
// freshly-provisioned database even though the login panel offers them.
//
// Chained after the base migration (shares the same Postgres). Idempotent, so
// re-running on each launch is a no-op once seeded. DOTNET_ENVIRONMENT is pinned
// to Development and Seed__DemoPassword passed explicitly because seed-demo
// refuses to run outside Development and requires the demo credential in config
// (mirrors the dashboard's DEMO_PASSWORD = "Password123!").
//
// IMPORTANT: the migrator is a generic-host console app, so its IHostEnvironment
// reads DOTNET_ENVIRONMENT — NOT ASPNETCORE_ENVIRONMENT (only ASP.NET web hosts
// honor that). Aspire injects ASPNETCORE_ENVIRONMENT into child projects but not
// DOTNET_ENVIRONMENT, so without this line the migrator defaults to Production
// and seed-demo bails with "REFUSING to run".
var demoSeeder = builder.AddProject<Projects.FSH_Starter_DbMigrator>("fsh-demo-seeder")
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
var api = builder.AddProject<Projects.FSH_Starter_Api>("fsh-api")
    .WithReference(postgres)
    .WithReference(redis)
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
    .WithEnvironment("Storage__Provider", "s3")
    .WithEnvironment("Storage__S3__Bucket", MinioBucket)
    .WithEnvironment("Storage__S3__Region", "us-east-1")
    .WithEnvironment("Storage__S3__ServiceUrl", minioApiEndpoint)
    .WithEnvironment("Storage__S3__AccessKey", minioUser)
    .WithEnvironment("Storage__S3__SecretKey", minioPassword)
    .WithEnvironment("Storage__S3__ForcePathStyle", "true")
    .WithEnvironment("Storage__S3__PublicBaseUrl", ReferenceExpression.Create($"{minioApiEndpoint}/{MinioBucket}"));

//#if (frontend)
// Admin console (React + Vite).
//
// We target the API's HTTPS endpoint, not HTTP. The API has
// UseHttpsRedirection(), so calls to the http endpoint bounce 307 to
// https — and the browser strips the Authorization header on cross-
// origin redirects (different scheme/port count as cross-origin per
// the Fetch spec). Going straight to https avoids the redirect and
// preserves the bearer token on every request.
builder.AddJavaScriptApp("fsh-admin", "../../../clients/admin", "dev")
    .WithNpm()
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5173, targetPort: 5173, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"));

// Tenant-facing dashboard (React + Vite, with SSE live feed)
builder.AddJavaScriptApp("fsh-dashboard", "../../../clients/dashboard", "dev")
    .WithNpm()
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5174, targetPort: 5174, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"));
//#endif

await builder.Build().RunAsync();
