using System.Globalization;
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
using FSH.Starter.DbMigrator.DemoSeed;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// FSH DbMigrator — one-shot console that migrates every DB to head, optionally seeds, then exits 0/1.
// Runs as a deployment step (not at API startup) so it can use an elevated-DDL connection string. Verbs: see MigratorCommand.HelpText.

var cli = MigratorCommand.Parse(args);
if (cli.Help)
{
    await Console.Out.WriteLineAsync(MigratorCommand.HelpText).ConfigureAwait(false);
    return 0;
}
var builder = Host.CreateApplicationBuilder(args);

// Disable build-time DI validation: auto-on in Development, it walks ALL descriptors incl. handlers this
// reduced-graph process never invokes (Chat→IHubContext, Identity→IMailService) and throws — false positive.
builder.ConfigureContainer(new DefaultServiceProviderFactory(
    new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false }));

// Under dotnet run the cwd is the project folder but appsettings.json is copied to the output dir,
// so load it from AppContext.BaseDirectory for IdentityModule's JwtOptions to validate.
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true);
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"), optional: true);

// Re-add environment variables and command line args so they maintain priority over the manually added JSON files.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// IdentityModule's JwtOptions.ValidateOnStart() trips on the empty SigningKey in base appsettings, but
// the migrator never mints JWTs. Inject a labelled placeholder only when nothing real is configured.
if (string.IsNullOrWhiteSpace(builder.Configuration["JwtOptions:SigningKey"]))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["JwtOptions:SigningKey"] = "fsh-dbmigrator-placeholder-never-mints-tokens-32+",
        ["JwtOptions:Issuer"] = builder.Configuration["JwtOptions:Issuer"] ?? "fsh.local",
        ["JwtOptions:Audience"] = builder.Configuration["JwtOptions:Audience"] ?? "fsh.clients",
    });
}

// Fail-fast with one clear line if DatabaseOptions__ConnectionString is unset, rather than letting
// host-build-time option validation throw a stack trace.
if (string.IsNullOrWhiteSpace(builder.Configuration["DatabaseOptions:ConnectionString"]))
{
    await Console.Error.WriteLineAsync(
        "[migrator] FAILED: DatabaseOptions:ConnectionString is empty — refusing to run against an unconfigured target. "
        + "Set DatabaseOptions__ConnectionString to an elevated-DDL connection string before invoking the migrator.")
        .ConfigureAwait(false);
    return 1;
}

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

// Disable runtime-only concerns; persistence + multitenancy stay on so DbInitializers resolve. Caching
// stays on because some modules' ctor wiring touches IDistributedCache (in-memory fallback if no Redis).
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

// TenantProvisioningService needs IJobService, but Hangfire's is gated behind EnableJobs (off here).
// Provide a throwing no-op so the DI graph resolves; the migration code paths don't enqueue jobs.
builder.Services.AddSingleton<FSH.Framework.Jobs.Services.IJobService, NoOpJobService>();

// Strip every BackgroundService (+ TenantStoreInitializerHostedService) before StartAsync: left running they
// poll/write tables BEFORE Step 1/2 create them (42P01). StartupValidator + Serilog flush IHostedServices stay.
foreach (var descriptor in builder.Services
    .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
        && (typeof(Microsoft.Extensions.Hosting.BackgroundService).IsAssignableFrom(d.ImplementationType)
            || d.ImplementationType?.Name == "TenantStoreInitializerHostedService"))
    .ToList())
{
    builder.Services.Remove(descriptor);
}

// DemoSeeder is opt-in via the `seed-demo` verb. Register unconditionally so
// the DI graph is satisfied; the verb dispatch below decides whether to call it.
builder.Services.AddScoped<DemoSeeder>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<MigratorCommand>>();

// Start the host so logging providers / option validators initialise.
await host.StartAsync().ConfigureAwait(false);

try
{
    // ── Step 0 — wait for the database to come up ────────────────────────
    // Postgres may still be initialising on cold-start; exp. backoff (≤2 min), then TimeoutException + exit 1.
    var connectionString = host.Services.GetRequiredService<IConfiguration>()["DatabaseOptions:ConnectionString"]
        ?? throw new InvalidOperationException("DatabaseOptions:ConnectionString is not configured.");
    await Console.Out.WriteLineAsync("[migrator] waiting for postgres…").ConfigureAwait(false);
    await PostgresMigratorLock.WaitForDatabaseAsync(connectionString, logger, CancellationToken.None)
        .ConfigureAwait(false);
    await Console.Out.WriteLineAsync("[migrator] postgres ready").ConfigureAwait(false);

    // Log the connected role + database so a misconfigured low-priv connection string surfaces now,
    // not as "permission denied for schema public" during MigrateAsync.
    await LogConnectionIdentityAsync(connectionString).ConfigureAwait(false);

    // ── Step 0b — acquire the advisory lock ──────────────────────────────
    // Session-level lock: concurrent runs block here; auto-releases on connection close (no orphan on crash).
    await Console.Out.WriteLineAsync("[migrator] acquiring advisory lock…").ConfigureAwait(false);
    await using var migratorLock = await PostgresMigratorLock
        .AcquireAsync(connectionString, logger, CancellationToken.None)
        .ConfigureAwait(false);
    await Console.Out.WriteLineAsync("[migrator] advisory lock acquired").ConfigureAwait(false);

    // ── Step 1 — tenant catalog ───────────────────────────────────────────
    // Always applied first: the per-tenant migrator below reads every tenant out of this database.
    using (var scope = host.Services.CreateScope())
    {
        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var pending = (await tenantDb.Database.GetPendingMigrationsAsync(CancellationToken.None)
            .ConfigureAwait(false)).ToList();

        if (cli.Command == "list-pending")
        {
            await Console.Out.WriteLineAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"[tenant-catalog] {pending.Count} pending migration(s)"))
                .ConfigureAwait(false);
            foreach (var name in pending)
            {
                await Console.Out.WriteLineAsync($"  · {name}").ConfigureAwait(false);
            }
        }
        else if (pending.Count > 0)
        {
            await Console.Out.WriteLineAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"[tenant-catalog] applying {pending.Count} migration(s)…"))
                .ConfigureAwait(false);
            await tenantDb.Database.MigrateAsync(CancellationToken.None).ConfigureAwait(false);
            await Console.Out.WriteLineAsync("[tenant-catalog] done").ConfigureAwait(false);
        }
        else
        {
            await Console.Out.WriteLineAsync("[tenant-catalog] already at head").ConfigureAwait(false);
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
            await Console.Out.WriteLineAsync("[tenant-catalog] seeded root tenant").ConfigureAwait(false);
        }
    }

    // ── Step 2 — per-tenant migrations + (optional) seeds ────────────────
    // `seed-demo` short-circuits this: it provisions its own demo tenants inline (Step 3 below).
    if (!cli.CatalogOnly && cli.Command != "seed-demo")
    {
        var tenantStore = host.Services.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenantService = host.Services.GetRequiredService<ITenantService>();

        var allTenants = (await tenantStore.GetAllAsync().ConfigureAwait(false)).ToList();
        var tenants = string.IsNullOrEmpty(cli.Tenant)
            ? allTenants
            : allTenants.Where(t => string.Equals(t.Id, cli.Tenant, StringComparison.OrdinalIgnoreCase)).ToList();

        if (tenants.Count == 0)
        {
            await Console.Out.WriteLineAsync($"[migrator] no tenants matched {cli.Tenant ?? "(all)"}")
                .ConfigureAwait(false);
        }

        foreach (var tenant in tenants)
        {
            if (cli.Command == "list-pending")
            {
                await Console.Out.WriteLineAsync(
                    $"[{tenant.Id}] migrations are evaluated per-tenant by each module's IDbInitializer")
                    .ConfigureAwait(false);
                continue;
            }
            if (cli.Command == "seed")
            {
                await Console.Out.WriteLineAsync($"[{tenant.Id}] seeding…").ConfigureAwait(false);
                await tenantService.SeedTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);
                continue;
            }

            await Console.Out.WriteLineAsync($"[{tenant.Id}] migrating…").ConfigureAwait(false);
            await tenantService.MigrateTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);

            if (cli.SeedAfter)
            {
                await Console.Out.WriteLineAsync($"[{tenant.Id}] seeding…").ConfigureAwait(false);
                await tenantService.SeedTenantAsync(tenant, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    // ── Step 3 — demo seed (verb: `seed-demo`) ───────────────────────────
    // Dev-only: provisions acme + globex with rich demo content; hard-fails outside Development.
    if (cli.Command == "seed-demo")
    {
        var env = host.Services.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
        {
            await Console.Error.WriteLineAsync(
                $"[demo-seed] REFUSING to run — DOTNET_ENVIRONMENT is '{env.EnvironmentName}'. "
                + "seed-demo is dev-only by design.")
                .ConfigureAwait(false);
            return 1;
        }

        await Console.Out.WriteLineAsync("[demo-seed] provisioning acme + globex with demo content…")
            .ConfigureAwait(false);
        using var scope = host.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
        await seeder.RunAsync(CancellationToken.None).ConfigureAwait(false);
        await Console.Out.WriteLineAsync("[demo-seed] done").ConfigureAwait(false);
    }

    await Console.Out.WriteLineAsync("[migrator] finished successfully.").ConfigureAwait(false);
    return 0;
}
#pragma warning disable CA1031 // Top-level Main intentionally catches every exception to convert any failure into exit code 1.
catch (Exception ex)
#pragma warning restore CA1031
{
    logger.LogError(ex, "DbMigrator failed");
    await Console.Error.WriteLineAsync($"[migrator] FAILED: {ex.GetType().Name}: {ex.Message}")
        .ConfigureAwait(false);
    if (ex.StackTrace is { } stack)
    {
        await Console.Error.WriteLineAsync(stack).ConfigureAwait(false);
    }
    return 1;
}
finally
{
    // Flush logging buffers + run host shutdown so the operator (and any
    // CI log collector) sees the final lines before the process exits.
    await host.StopAsync().ConfigureAwait(false);
}

static async Task LogConnectionIdentityAsync(string connectionString)
{
    // Best-effort identity probe — never fail the migrator over a logging step.
    try
    {
        await using var conn = new Npgsql.NpgsqlConnection(connectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT current_user, current_database()";
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        if (await reader.ReadAsync().ConfigureAwait(false))
        {
            var role = reader.GetString(0);
            var db = reader.GetString(1);
            await Console.Out.WriteLineAsync(string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"[migrator] connected as role={role} database={db}")).ConfigureAwait(false);
        }
    }
#pragma warning disable CA1031 // Logging-only path: any exception swallowed and reported, never fatal.
    catch (Exception ex)
#pragma warning restore CA1031
    {
        await Console.Out.WriteLineAsync($"[migrator] WARN: could not log connection identity: {ex.Message}")
            .ConfigureAwait(false);
    }
}
