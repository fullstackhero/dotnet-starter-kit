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

// Suppress the API-only background services so the migrator doesn't try to
// start the tenant-store seeder twice (it runs explicitly below) and the
// background workers (Hangfire, etc.) don't fight for resources.
builder.Services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<MigratorCommand>>();

try
{
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
            logger.LogInformation("[tenant-catalog] applying {Count} migration(s)…", pending.Count);
            await tenantDb.Database.MigrateAsync(CancellationToken.None).ConfigureAwait(false);
            logger.LogInformation("[tenant-catalog] done");
        }
        else
        {
            logger.LogInformation("[tenant-catalog] already at head");
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
            logger.LogInformation("[tenant-catalog] seeded root tenant");
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
            logger.LogWarning("No tenants matched {Tenant}", cli.Tenant ?? "(all)");
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
                logger.LogInformation("[{Tenant}] seeding…", tenant.Id);
                await tenantService.SeedTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);
                continue;
            }

            logger.LogInformation("[{Tenant}] migrating…", tenant.Id);
            await tenantService.MigrateTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);

            if (cli.SeedAfter)
            {
                logger.LogInformation("[{Tenant}] seeding…", tenant.Id);
                await tenantService.SeedTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    logger.LogInformation("DbMigrator finished successfully.");
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "DbMigrator failed");
    return 1;
}
