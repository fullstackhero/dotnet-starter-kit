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
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using FSH.Modules.Multitenancy.Data;
using FSH.Framework.Web.Modules;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace Integration.Tests.Infrastructure;

public sealed class FshWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string S3AccessKey = "rustfsadmin";
    private const string S3SecretKey = "rustfsadmin";
    private const string S3Bucket = "fsh-integration-test-uploads";

    private static readonly SemaphoreSlim _migrationLock = new(1, 1);
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fsh_integration_tests")
        .WithUsername("postgres")
        .WithPassword("integration_test_pwd")
        .WithAutoRemove(true)
        .WithCleanUp(true)
        .Build();

    // RustFS (S3-compatible) replaces MinIO, whose images were withdrawn from Docker Hub and quay.io.
    private const int S3Port = 9000;
    private readonly IContainer _s3 = new ContainerBuilder("rustfs/rustfs:1.0.0")
        .WithPortBinding(S3Port, true)
        .WithEnvironment("RUSTFS_ACCESS_KEY", S3AccessKey)
        .WithEnvironment("RUSTFS_SECRET_KEY", S3SecretKey)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(S3Port).ForPath("/health")))
        .WithAutoRemove(true)
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _s3.StartAsync());
        await CreateS3BucketAsync();

        // Force host creation via the Server property (no leaked HttpClient)
        _ = Server;

        // Migrate + seed the root tenant. The semaphore stops test classes sharing the DB
        // from migrating simultaneously.
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
        await _s3.DisposeAsync();
    }

    /// <summary>The S3 (RustFS) endpoint URL exposed to the host configuration; useful for tests that need to PUT bytes directly.</summary>
    public string S3ServiceUrl => $"http://{_s3.Hostname}:{_s3.GetMappedPublicPort(S3Port)}";

    private async Task CreateS3BucketAsync()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = S3ServiceUrl,
            ForcePathStyle = true,
            UseHttp = true,
            AuthenticationRegion = "us-east-1"
        };

        using var client = new AmazonS3Client(
            new Amazon.Runtime.BasicAWSCredentials(S3AccessKey, S3SecretKey),
            config);

        try
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = S3Bucket });
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
                ["RateLimitingOptions:Enabled"] = "false",
                ["PasswordPolicy:EnforcePasswordExpiry"] = "false",
                ["Seed:DemoPassword"] = "Password123!",
                ["Seed:DefaultAdminPassword"] = TestConstants.DefaultPassword,
                ["SecurityHeadersOptions:Enabled"] = "false",
                ["Storage:Provider"] = "s3",
                ["Storage:S3:Bucket"] = S3Bucket,
                ["Storage:S3:ServiceUrl"] = S3ServiceUrl,
                ["Storage:S3:AccessKey"] = S3AccessKey,
                ["Storage:S3:SecretKey"] = S3SecretKey,
                ["Storage:S3:ForcePathStyle"] = "true",
                ["Storage:S3:PublicRead"] = "false",
                ["Storage:S3:Region"] = "us-east-1",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Stamp the connection IP from a test header so forwarded-headers trust checks are testable.
            services.AddSingleton<IStartupFilter, TestRemoteIpStartupFilter>();

            // The production TrustedProxyOptions read happens eagerly, before the test config overlay
            // applies (same quirk as storage below), so bind the trusted upstream here instead. Note what
            // that leaves these tests covering: overwriting the flags, the forward limit and both trust
            // lists wholesale replaces whatever the TrustedProxyOptions binding produced, so what runs
            // against TestConstants.TrustedProxyIp is the real middleware and the real placement of
            // UseForwardedHeaders, not the binding that feeds them in production. That binding is gated
            // separately, by Framework.Tests/Web/TrustedProxyOptionsBindingTests.
            services.PostConfigure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(forwarded =>
            {
                forwarded.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                    | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
                forwarded.ForwardLimit = 1;
                forwarded.KnownProxies.Clear();
                forwarded.KnownIPNetworks.Clear();
                forwarded.KnownProxies.Add(System.Net.IPAddress.Parse(TestConstants.TrustedProxyIp));
            });

            // Remove hosted services that need unavailable infra or race migrations (RolePermissionSync,
            // Hangfire server + stale-lock cleanup, OutboxDispatcher); we register our own InMemory server below.
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

            // Detailed errors in tests instead of generic "An unexpected error occurred"
            var existingHandlers = services.Where(d =>
                d.ServiceType == typeof(Microsoft.AspNetCore.Diagnostics.IExceptionHandler)).ToList();
            foreach (var h in existingHandlers) services.Remove(h);
            services.AddExceptionHandler<DetailedTestExceptionHandler>();

            // AddHeroStorage reads `Storage:Provider` eagerly (before the test config overlay applies), so it
            // wires LocalStorageService. Replace it here with the S3 stack pointed at the RustFS testcontainer.
            RewireStorageForS3(services);
        });
    }

    private void RewireStorageForS3(IServiceCollection services)
    {
        var toRemove = services
            .Where(d => d.ServiceType == typeof(FSH.Framework.Storage.Services.IStorageService)
                     || d.ServiceType == typeof(FSH.Framework.Storage.Local.LocalStorageService)
                     || d.ServiceType == typeof(FSH.Framework.Storage.S3.S3StorageService)
                     || d.ServiceType == typeof(IAmazonS3))
            .ToList();
        foreach (var d in toRemove) services.Remove(d);

        services.Configure<FSH.Framework.Storage.S3.S3StorageOptions>(opts =>
        {
            opts.Bucket = S3Bucket;
            opts.ServiceUrl = S3ServiceUrl;
            opts.AccessKey = S3AccessKey;
            opts.SecretKey = S3SecretKey;
            opts.ForcePathStyle = true;
            opts.PublicRead = false;
            opts.Region = "us-east-1";
        });

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = S3ServiceUrl,
                ForcePathStyle = true,
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            };
            return new AmazonS3Client(
                new Amazon.Runtime.BasicAWSCredentials(S3AccessKey, S3SecretKey),
                config);
        });
        services.AddTransient<FSH.Framework.Storage.S3.S3StorageService>();
        services.AddTransient<FSH.Framework.Storage.Services.IStorageService>(sp =>
            sp.GetRequiredService<FSH.Framework.Storage.S3.S3StorageService>());
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
}
