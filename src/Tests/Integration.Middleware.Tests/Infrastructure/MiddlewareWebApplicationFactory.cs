using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Data;
using FSH.Framework.Web.Modules;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace Integration.Middleware.Tests.Infrastructure;

/// <summary>
/// Single-host factory for the real-wiring middleware tests. Unlike Integration.Tests'
/// FshWebApplicationFactory, this one:
/// 1. Keeps the production <see cref="FSH.Framework.Web.Exceptions.GlobalExceptionHandler"/>
///    (no DetailedTestExceptionHandler swap), so unhandled exceptions produce RFC 9457 output.
/// 2. Enables rate limiting with a tiny auth window so the auth limiter trips deterministically
///    while the global tenant/user/ip limiters stay effectively unlimited.
/// 3. Enables security headers.
/// 4. Appends an AllowAnonymous GET /__test/throw endpoint (via the live route builder) that
///    throws a raw exception for the GlobalExceptionHandler to catch.
///
/// CRITICAL: This factory NEVER calls WithWebHostBuilder — exactly ONE host is built per process.
/// </summary>
public sealed class MiddlewareWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string MinioAccessKey = "minioadmin";
    private const string MinioSecretKey = "minioadmin";
    private const string MinioBucket = "fsh-middleware-test-uploads";

    private static readonly SemaphoreSlim _migrationLock = new(1, 1);
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fsh_middleware_tests")
        .WithUsername("postgres")
        .WithPassword("integration_test_pwd")
        .WithAutoRemove(true)
        .WithCleanUp(true)
        .Build();

    private readonly MinioContainer _minio = new MinioBuilder("minio/minio:latest")
        .WithUsername(MinioAccessKey)
        .WithPassword(MinioSecretKey)
        .WithAutoRemove(true)
        .WithCleanUp(true)
        .Build();

    public MiddlewareWebApplicationFactory()
    {
        // AddHeroRateLimiting reads config EAGERLY at registration (before ConfigureWebHost's overlay merges)
        // and appsettings ships Enabled=false; set env vars here (in the up-front config) to flip it. Cf. AddHeroStorage.
        Environment.SetEnvironmentVariable("RateLimitingOptions__Enabled", "true");
        Environment.SetEnvironmentVariable("RateLimitingOptions__Auth__PermitLimit", "3");
        Environment.SetEnvironmentVariable("RateLimitingOptions__Auth__WindowSeconds", "300");
        Environment.SetEnvironmentVariable("RateLimitingOptions__Auth__QueueLimit", "0");
        Environment.SetEnvironmentVariable("RateLimitingOptions__Ip__PermitLimit", "100000");
        Environment.SetEnvironmentVariable("RateLimitingOptions__User__PermitLimit", "100000");
        Environment.SetEnvironmentVariable("RateLimitingOptions__Tenant__PermitLimit", "100000");
        Environment.SetEnvironmentVariable("SecurityHeadersOptions__Enabled", "true");
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());
        await CreateMinioBucketAsync();

        // Force host creation via the Server property (no leaked HttpClient)
        _ = Server;

        // Migrate + seed the root tenant; the semaphore stops test classes sharing a DB from
        // migrating simultaneously.
        await _migrationLock.WaitAsync();
        try
        {
            await ProvisionRootTenantAsync();
        }
        finally
        {
            _migrationLock.Release();
        }
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _minio.DisposeAsync();
    }

    /// <summary>The MinIO endpoint URL exposed to the host configuration; useful for tests that need to PUT bytes directly.</summary>
    public string MinioServiceUrl => _minio.GetConnectionString();

    private async Task CreateMinioBucketAsync()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = _minio.GetConnectionString(),
            ForcePathStyle = true,
            UseHttp = true,
            AuthenticationRegion = "us-east-1"
        };

        using var client = new AmazonS3Client(
            new Amazon.Runtime.BasicAWSCredentials(MinioAccessKey, MinioSecretKey),
            config);

        try
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = MinioBucket });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou" || ex.ErrorCode == "BucketAlreadyExists")
        {
            // Idempotent across factory re-creations.
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseOptions:Provider"] = "POSTGRESQL",
                ["DatabaseOptions:ConnectionString"] = _postgres.GetConnectionString(),
                ["DatabaseOptions:MigrationsAssembly"] = "FSH.Starter.Migrations.PostgreSQL",
                ["CachingOptions:Redis"] = "",
                ["JwtOptions:Issuer"] = TestConstants.JwtIssuer,
                ["JwtOptions:Audience"] = TestConstants.JwtAudience,
                ["JwtOptions:SigningKey"] = TestConstants.JwtSigningKey,
                ["JwtOptions:AccessTokenMinutes"] = "30",
                ["JwtOptions:RefreshTokenDays"] = "7",
                ["OriginOptions:OriginUrl"] = "http://localhost",
                ["OpenTelemetryOptions:Enabled"] = "false",
                ["EventingOptions:UseHostedServiceDispatcher"] = "false",
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"] = "Fatal",
                ["Serilog:MinimumLevel:Override:Npgsql"] = "Fatal",
                ["Serilog:WriteTo:0:Name"] = "Console",
                ["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"] = "Warning",
                ["Serilog:WriteTo:1:Name"] = "",
                ["MailOptions:UseSendGrid"] = "false",
                ["HangfireOptions:Username"] = "admin",
                ["HangfireOptions:Password"] = "integration-test-hangfire-pwd",
                ["HangfireOptions:Route"] = "/jobs",
                ["PasswordPolicy:EnforcePasswordExpiry"] = "false",
                ["Seed:DemoPassword"] = "Password123!",
                ["Seed:DefaultAdminPassword"] = TestConstants.DefaultPassword,

                // Middleware-test overrides: rate limiting ON, but only the "auth" policy should trip —
                // the global tenant/user/ip limiters are set unlimited so they don't interfere.
                ["RateLimitingOptions:Enabled"] = "true",
                ["RateLimitingOptions:Auth:PermitLimit"] = "3",
                ["RateLimitingOptions:Auth:WindowSeconds"] = "300",
                ["RateLimitingOptions:Auth:QueueLimit"] = "0",
                ["RateLimitingOptions:Ip:PermitLimit"] = "100000",
                ["RateLimitingOptions:User:PermitLimit"] = "100000",
                ["RateLimitingOptions:Tenant:PermitLimit"] = "100000",

                // Security headers ON so SecurityHeadersMiddleware emits its headers.
                ["SecurityHeadersOptions:Enabled"] = "true",

                ["Storage:Provider"] = "s3",
                ["Storage:S3:Bucket"] = MinioBucket,
                ["Storage:S3:ServiceUrl"] = _minio.GetConnectionString(),
                ["Storage:S3:AccessKey"] = MinioAccessKey,
                ["Storage:S3:SecretKey"] = MinioSecretKey,
                ["Storage:S3:ForcePathStyle"] = "true",
                ["Storage:S3:PublicRead"] = "false",
                ["Storage:S3:Region"] = "us-east-1",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove hosted services that need unavailable infra or race migrations: RolePermissionSync,
            // OutboxDispatcher (query tables pre-migration), and Hangfire server/cleanup (InMemory below).
            var hostedServicesToRemove = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                    (d.ImplementationType?.Name == "RolePermissionSyncHostedService" ||
                     d.ImplementationType?.FullName?.Contains("Hangfire", StringComparison.Ordinal) == true ||
                     d.ImplementationType?.Name == "HangfireStaleLockCleanupService" ||
                     d.ImplementationType?.Name == "OutboxDispatcherHostedService"))
                .ToList();
            foreach (var service in hostedServicesToRemove)
            {
                services.Remove(service);
            }

            services.AddHangfire(config => config.UseInMemoryStorage());
            services.AddHangfireServer(options =>
            {
                options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
                options.Queues = ["default", "email"];
                options.WorkerCount = 2;
            });
            services.TryAddTransient<IJobService, HangfireService>();

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options => options.RequireHttpsMetadata = false);

            // Replace real mail service with a no-op to avoid SMTP errors and Hangfire retries
            services.RemoveAll<IMailService>();
            services.AddSingleton<IMailService, NoOpMailService>();

            // DELIBERATELY keep the production GlobalExceptionHandler (no swap) so /__test/throw yields real
            // RFC 9457 output; storage stays unrewired (no test hits it; MinIO + S3 keys kept so host binds).

            // Append a throwing endpoint via IStartupFilter: run next(app) first (UseRouting stamps the real
            // IEndpointRouteBuilder into app.Properties), then MapGet onto it — lazy data sources still match.
            services.AddSingleton<IStartupFilter, ThrowEndpointStartupFilter>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Disable ValidateOnBuild for .NET 10 minimal API dual-host model
        builder.UseServiceProviderFactory(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = false,
            ValidateScopes = false
        }));

        ResetModuleLoader();
        return base.CreateHost(builder);
    }

    private async Task ProvisionRootTenantAsync()
    {
        // 1. Explicitly migrate the tenant catalog FIRST.
        using (var scope = Services.CreateScope())
        {
            var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            await tenantDbContext.Database.MigrateAsync();

            // 2. Seed Root Tenant if missing (ensures we don't wait for background service)
            var rootTenant = await tenantDbContext.TenantInfo.FindAsync(MultitenancyConstants.Root.Id);
            if (rootTenant is null)
            {
                rootTenant = new AppTenantInfo(
                    MultitenancyConstants.Root.Id,
                    MultitenancyConstants.Root.Name,
                    string.Empty,
                    MultitenancyConstants.Root.EmailAddress,
                    issuer: MultitenancyConstants.Root.Issuer);

                var validUpto = DateTime.UtcNow.AddYears(1);
                rootTenant.SetValidity(validUpto);
                await tenantDbContext.TenantInfo.AddAsync(rootTenant);
                await tenantDbContext.SaveChangesAsync();
            }

            // 3. Run all module migrations (identity, audit, webhook schemas)
            var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(rootTenant);

            foreach (var init in scope.ServiceProvider.GetServices<IDbInitializer>())
            {
                await init.MigrateAsync(CancellationToken.None);
            }

            // 4. Seed all modules (admin user, roles, permissions, groups)
            foreach (var init in scope.ServiceProvider.GetServices<IDbInitializer>())
            {
                await init.SeedAsync(CancellationToken.None);
            }

            // 5. Run the role-permission syncer through the production code path.
            var syncer = scope.ServiceProvider.GetRequiredService<FSH.Modules.Identity.Authorization.RolePermissionSyncer>();
            await syncer.SyncAsync(CancellationToken.None);
        }
    }

    private static void ResetModuleLoader()
    {
        var type = typeof(ModuleLoader);
        var modulesField = type.GetField("_modules", BindingFlags.Static | BindingFlags.NonPublic);
        var loadedField = type.GetField("_modulesLoaded", BindingFlags.Static | BindingFlags.NonPublic);

        if (modulesField?.GetValue(null) is System.Collections.IList list)
        {
            list.Clear();
        }

        loadedField?.SetValue(null, false);
    }

    /// <summary>
    /// Appends an AllowAnonymous GET /__test/throw endpoint to the LIVE route builder.
    ///
    /// A DI-registered EndpointDataSource is NOT picked up by minimal hosting's implicit
    /// routing — the route builder must be the one the host actually maps against. The
    /// WebApplication stashes that builder in <c>app.Properties["__EndpointRouteBuilder"]</c>
    /// once <c>UseRouting()</c> has run. We therefore call <c>next(app)</c> first (so Program.cs's
    /// pipeline — including UseRouting — executes and populates the property) and only then reach
    /// in and MapGet. Route-builder data sources are evaluated lazily on the first request, so
    /// appending after the pipeline is built still results in a matched, AllowAnonymous endpoint.
    /// </summary>
    private sealed class ThrowEndpointStartupFilter : IStartupFilter
    {
        private const string EndpointRouteBuilderKey = "__EndpointRouteBuilder";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            ArgumentNullException.ThrowIfNull(next);

            return app =>
            {
                next(app);

                if (app.Properties.TryGetValue(EndpointRouteBuilderKey, out var value)
                    && value is IEndpointRouteBuilder routeBuilder)
                {
                    routeBuilder.MapGet("/__test/throw", (HttpContext _) =>
                        {
                            throw new InvalidOperationException("boom");
                        })
                        .AllowAnonymous();
                }
            };
        }
    }
}
