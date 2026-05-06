using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Authorization;

/// <summary>
/// Runs once on host startup: iterates every tenant and adds any permission claims that
/// have been registered via <see cref="FSH.Framework.Shared.Constants.PermissionConstants"/>
/// but are missing from the role claims table for that tenant. Idempotent and lightweight —
/// only writes when there's something new, so it's safe to run unconditionally.
/// </summary>
/// <remarks>
/// Implemented as a <see cref="BackgroundService"/> rather than <see cref="IHostedService"/>
/// because the tenant catalog DB is migrated by another <see cref="BackgroundService"/>
/// (<c>TenantStoreInitializerHostedService</c>). All <c>BackgroundService.ExecuteAsync</c>
/// methods run in parallel after every <c>IHostedService.StartAsync</c> returns, so we
/// poll the tenant store until it's ready before running the sync.
/// </remarks>
internal sealed class RolePermissionSyncHostedService(
    IServiceProvider serviceProvider,
    ILogger<RolePermissionSyncHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var tenants = await WaitForTenantsAsync(stoppingToken).ConfigureAwait(false);
            if (tenants is null)
            {
                logger.LogWarning(
                    "Role permission sync skipped — tenant catalog was not ready within {Timeout}",
                    MaxWait);
                return;
            }

            foreach (var tenant in tenants)
            {
                if (stoppingToken.IsCancellationRequested) break;
                await SyncTenantAsync(tenant, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Permission sync is best-effort — never crash the host over it.
            logger.LogError(ex, "Role permission sync failed; new permissions may not be available until next sync");
        }
    }

    /// <summary>
    /// Polls the tenant store until tenants are returnable (catalog DB migrated and at
    /// least the root tenant seeded). Returns null if the deadline is exceeded.
    /// </summary>
    private async Task<IEnumerable<AppTenantInfo>?> WaitForTenantsAsync(CancellationToken stoppingToken)
    {
        var deadline = DateTimeOffset.UtcNow + MaxWait;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (stoppingToken.IsCancellationRequested) return null;

            try
            {
                using var scope = serviceProvider.CreateScope();
                var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
                var tenants = (await tenantStore.GetAllAsync().ConfigureAwait(false)).ToList();
                if (tenants.Count > 0)
                {
                    return tenants;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Catalog DB likely not migrated yet — keep waiting.
                logger.LogDebug(ex, "Tenant store not ready yet; retrying in {Interval}", PollInterval);
            }

            await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
        }

        return null;
    }

    private async Task SyncTenantAsync(AppTenantInfo tenant, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            var syncer = scope.ServiceProvider.GetRequiredService<RolePermissionSyncer>();
            await syncer.SyncAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Per-tenant failure must not stop the rest of the loop.
            logger.LogError(ex, "Role permission sync failed for tenant '{Tenant}'", tenant.Id);
        }
    }
}
