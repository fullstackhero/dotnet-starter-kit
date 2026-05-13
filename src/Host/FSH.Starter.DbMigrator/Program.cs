using System.Reflection;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web;
using FSH.Framework.Web.Modules;
using FSH.Modules.Auditing;
using FSH.Modules.Billing;
using FSH.Modules.Catalog;
using FSH.Modules.Identity;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;
using FSH.Modules.Tickets;
using FSH.Modules.Webhooks;
using FSH.Starter.DbMigrator;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ─────────────────────────────────────────────────────────────────────────
// FSH DbMigrator — one-shot console that brings every database in the
// solution up to head, optionally seeds, then exits with code 0 on success
// or 1 on any failure. Runs as a deployment step (not at API startup), so
// the runtime app can use a reduced-privilege connection string and the
// migrator runs with elevated DDL perms.
//
// Usage:
//   dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply
//   dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply --tenant root
//   dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply --catalog-only
//   dotnet run --project src/Host/FSH.Starter.DbMigrator -- list-pending
//   dotnet run --project src/Host/FSH.Starter.DbMigrator -- seed
// ─────────────────────────────────────────────────────────────────────────

var cli = MigratorCommand.Parse(args);
if (cli.Help)
{
    Console.WriteLine(MigratorCommand.HelpText);
    return 0;
}

var builder = Host.CreateApplicationBuilder(args);

// Mirror the API's mediator registration so module handlers wire correctly —
// some module DbInitializers depend on services that mediator pipelines build.
builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies =
    [
        typeof(GenerateTokenCommand),
        typeof(GenerateTokenCommandHandler),
        typeof(GetTenantStatusQuery),
        typeof(GetTenantStatusQueryHandler),
        typeof(FSH.Modules.Auditing.Contracts.AuditEnvelope),
        typeof(FSH.Modules.Auditing.Persistence.AuditDbContext),
        typeof(FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription.CreateWebhookSubscriptionCommand),
        typeof(FSH.Modules.Webhooks.WebhooksModule),
        typeof(FSH.Modules.Billing.Contracts.BillingContractsMarker),
        typeof(FSH.Modules.Billing.BillingModule),
        typeof(FSH.Modules.Catalog.Contracts.CatalogContractsMarker),
        typeof(FSH.Modules.Catalog.CatalogModule),
        typeof(FSH.Modules.Tickets.Contracts.TicketsContractsMarker),
        typeof(FSH.Modules.Tickets.TicketsModule),
        typeof(FSH.Modules.Files.Contracts.v1.Commands.RequestUploadUrlCommand),
        typeof(FSH.Modules.Files.FilesModule),
        typeof(FSH.Modules.Chat.Contracts.v1.Commands.CreateChannelCommand),
        typeof(FSH.Modules.Chat.ChatModule),
        typeof(FSH.Modules.Notifications.Contracts.v1.Commands.MarkNotificationReadCommand),
        typeof(FSH.Modules.Notifications.NotificationsModule),
    ];
});

var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof(FSH.Modules.Files.FilesModule).Assembly,
    typeof(WebhooksModule).Assembly,
    typeof(BillingModule).Assembly,
    typeof(CatalogModule).Assembly,
    typeof(TicketsModule).Assembly,
    typeof(FSH.Modules.Chat.ChatModule).Assembly,
    typeof(FSH.Modules.Notifications.NotificationsModule).Assembly,
};

// Disable every runtime-only concern. Persistence + multitenancy stay enabled
// so the modules' DbInitializers resolve cleanly. Caching is left on because
// some modules' constructor wiring touches IDistributedCache; tolerate the
// in-memory fallback when Redis isn't configured.
builder.AddHeroPlatform(o =>
{
    o.EnableOpenTelemetry = false;
    o.EnableCors = false;
    o.EnableOpenApi = false;
    o.EnableJobs = false;
    o.EnableMailing = false;
    o.EnableSse = false;
    o.EnableRealtime = false;
    o.EnableQuotas = false;
    o.EnableFeatureFlags = false;
    o.EnableIdempotency = false;
    o.EnableCaching = true;
});

builder.AddModules(moduleAssemblies);

// The Multitenancy module's TenantProvisioningService depends on IJobService —
// Hangfire's IJobService is only registered when EnableJobs is true, which we
// don't want in the migrator (Hangfire needs its own schema bootstrap + workers).
// Provide a throwing no-op so the DI graph resolves; the migration code paths
// don't enqueue jobs.
builder.Services.AddSingleton<FSH.Framework.Jobs.Services.IJobService, NoOpJobService>();

// Surgically remove the only hosted service that would race with this
// migrator — TenantStoreInitializerHostedService also calls MigrateAsync
// on the tenant catalog. The advisory lock + EF's __EFMigrationsHistory
// would make it eventually safe, but having two writers fight is ugly in
// the logs. Hangfire, etc. are already disabled via EnableJobs=false on
// AddHeroPlatform above. Serilog's flush-on-shutdown hosted service is
// preserved so log output actually reaches stdout/stderr.
foreach (var descriptor in builder.Services
    .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
        && d.ImplementationType?.Name == "TenantStoreInitializerHostedService")
    .ToList())
{
    builder.Services.Remove(descriptor);
}

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<MigratorCommand>>();

// Start the host so logging providers / option validators initialise.
await host.StartAsync().ConfigureAwait(false);

try
{
    // ── Step 0 — wait for the database to come up ────────────────────────
    // Aspire / Kubernetes cold-starts can leave Postgres still initialising
    // when the migrator's container is scheduled. Exponential backoff up to
    // two minutes catches the common 5–30s startup gap; longer outages
    // surface as a clean TimeoutException + exit code 1.
    var connectionString = host.Services.GetRequiredService<IConfiguration>()["DatabaseOptions:ConnectionString"]
        ?? throw new InvalidOperationException("DatabaseOptions:ConnectionString is not configured.");
    Console.WriteLine("[migrator] waiting for postgres…");
    await PostgresMigratorLock.WaitForDatabaseAsync(connectionString, logger, CancellationToken.None)
        .ConfigureAwait(false);
    Console.WriteLine("[migrator] postgres ready");

    // ── Step 0b — acquire the advisory lock ──────────────────────────────
    // Postgres session-level advisory lock. Concurrent migrator runs
    // (CI-races, ops-vs-Helm-hook overlap, replicas: 2 by accident) block
    // here until the holder finishes. The lock auto-releases on connection
    // close, so even a crashed migrator doesn't leave the lock orphaned.
    Console.WriteLine("[migrator] acquiring advisory lock…");
    await using var migratorLock = await PostgresMigratorLock
        .AcquireAsync(connectionString, logger, CancellationToken.None)
        .ConfigureAwait(false);
    Console.WriteLine("[migrator] advisory lock acquired");

    // ── Step 1 — tenant catalog ───────────────────────────────────────────
    // Always applied first because the per-tenant migrator below reads
    // every tenant out of this database.
    using (var scope = host.Services.CreateScope())
    {
        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var pending = (await tenantDb.Database.GetPendingMigrationsAsync(CancellationToken.None)
            .ConfigureAwait(false)).ToList();

        if (cli.Command == "list-pending")
        {
            Console.WriteLine($"[tenant-catalog] {pending.Count} pending migration(s)");
            foreach (var name in pending) Console.WriteLine($"  · {name}");
        }
        else if (pending.Count > 0)
        {
            Console.WriteLine($"[tenant-catalog] applying {pending.Count} migration(s)…");
            await tenantDb.Database.MigrateAsync(CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine("[tenant-catalog] done");
        }
        else
        {
            Console.WriteLine("[tenant-catalog] already at head");
        }

        // Seed the root tenant the first time the catalog comes up so the
        // per-tenant pass below has at least one tenant to iterate.
        var seeded = await tenantDb.TenantInfo
            .FindAsync([MultitenancyConstants.Root.Id], CancellationToken.None)
            .ConfigureAwait(false);
        if (seeded is null && cli.Command != "list-pending")
        {
            var rootTenant = new AppTenantInfo(
                MultitenancyConstants.Root.Id,
                MultitenancyConstants.Root.Name,
                connectionString: string.Empty,
                MultitenancyConstants.Root.EmailAddress,
                issuer: MultitenancyConstants.Root.Issuer);
            rootTenant.SetValidity(TimeProvider.System.GetUtcNow().UtcDateTime.AddYears(1));
            await tenantDb.TenantInfo.AddAsync(rootTenant, CancellationToken.None).ConfigureAwait(false);
            await tenantDb.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine("[tenant-catalog] seeded root tenant");
        }
    }

    // ── Step 2 — per-tenant migrations + (optional) seeds ────────────────
    if (!cli.CatalogOnly)
    {
        var tenantStore = host.Services.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenantService = host.Services.GetRequiredService<ITenantService>();

        var allTenants = (await tenantStore.GetAllAsync().ConfigureAwait(false)).ToList();
        var tenants = string.IsNullOrEmpty(cli.Tenant)
            ? allTenants
            : allTenants.Where(t => string.Equals(t.Id, cli.Tenant, StringComparison.OrdinalIgnoreCase)).ToList();

        if (tenants.Count == 0)
        {
            Console.WriteLine($"[migrator] no tenants matched {cli.Tenant ?? "(all)"}");
        }

        foreach (var tenant in tenants)
        {
            if (cli.Command == "list-pending")
            {
                Console.WriteLine($"[{tenant.Id}] migrations are evaluated per-tenant by each module's IDbInitializer");
                continue;
            }
            if (cli.Command == "seed")
            {
                Console.WriteLine($"[{tenant.Id}] seeding…");
                await tenantService.SeedTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);
                continue;
            }

            Console.WriteLine($"[{tenant.Id}] migrating…");
            await tenantService.MigrateTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);

            if (cli.SeedAfter)
            {
                Console.WriteLine($"[{tenant.Id}] seeding…");
                await tenantService.SeedTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    Console.WriteLine("[migrator] finished successfully.");
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "DbMigrator failed");
    Console.Error.WriteLine($"[migrator] FAILED: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}
finally
{
    // Flush logging buffers + run host shutdown so the operator (and any
    // CI log collector) sees the final lines before the process exits.
    await host.StopAsync().ConfigureAwait(false);
}
