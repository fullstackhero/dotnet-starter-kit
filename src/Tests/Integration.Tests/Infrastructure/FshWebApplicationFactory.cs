using System.Reflection;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
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
using Testcontainers.PostgreSql;

namespace Integration.Tests.Infrastructure;

public sealed class FshWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("fsh_integration_tests")
        .WithUsername("postgres")
        .WithPassword("integration_test_pwd")
        .WithAutoRemove(true)
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Force host creation via the Server property (no leaked HttpClient)
        _ = Server;

        // Run migrations and seed data for the root tenant.
        // We do this explicitly rather than relying on Hangfire background jobs
        // to guarantee deterministic ordering: migrate ALL schemas first, then seed.
        await ProvisionRootTenantAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
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
                ["MultitenancyOptions:RunTenantMigrationsOnStartup"] = "false",
                ["MultitenancyOptions:AutoProvisionOnStartup"] = "false",
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
                ["SecurityHeadersOptions:Enabled"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace Hangfire: use InMemory storage and a single fast-polling server.
            // Remove ALL existing Hangfire hosted services (production registers a 30s-polling
            // server + stale lock cleanup that tries to hit PostgreSQL Hangfire schema).
            // Remove hosted services that depend on infrastructure not available in tests:
            // - Hangfire server + stale lock cleanup (we register our own InMemory server below)
            // - OutboxDispatcherHostedService (queries OutboxMessages table before migrations run)
            var hostedServicesToRemove = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                    && (d.ImplementationType?.FullName?.Contains("Hangfire", StringComparison.Ordinal) == true
                        || d.ImplementationType?.Name == "HangfireStaleLockCleanupService"
                        || d.ImplementationType?.Name == "OutboxDispatcherHostedService"))
                .ToList();
            foreach (var svc in hostedServicesToRemove) services.Remove(svc);

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
        // Wait for TenantStoreInitializerHostedService (BackgroundService) to
        // migrate the tenant catalog and seed the root tenant.
        AppTenantInfo? rootTenant = null;
        for (int i = 0; i < 60; i++)
        {
            try
            {
                using var scope = Services.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
                rootTenant = await store.GetAsync(MultitenancyConstants.Root.Id);
                if (rootTenant is not null) break;
            }
            catch (Exception) when (i < 59)
            {
                // Tenant catalog DB not yet migrated — retry
            }

            await Task.Delay(500);
        }

        if (rootTenant is null)
        {
            throw new TimeoutException("Root tenant was not seeded within 30 seconds.");
        }

        // Run all module migrations (identity, audit, webhook schemas)
        using (var scope = Services.CreateScope())
        {
            var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(rootTenant);

            foreach (var init in scope.ServiceProvider.GetServices<IDbInitializer>())
            {
                await init.MigrateAsync(CancellationToken.None);
            }
        }

        // Seed all modules (admin user, roles, permissions, groups)
        using (var scope = Services.CreateScope())
        {
            var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(rootTenant);

            foreach (var init in scope.ServiceProvider.GetServices<IDbInitializer>())
            {
                await init.SeedAsync(CancellationToken.None);
            }
        }

        // Run the role-permission syncer through the production code path.
        // Catches regressions where new module permissions never reach existing
        // tenants (the bug that produced 401s in dev when the Catalog module was added).
        using (var scope = Services.CreateScope())
        {
            var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(rootTenant);

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
