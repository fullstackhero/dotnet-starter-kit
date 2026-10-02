using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Files.Jobs;

/// <summary>
/// Hourly purge of FileAsset rows stuck in PendingUpload past their UploadDeadline, in every tenant.
/// Best-effort removal of any bytes that did make it to storage. No quota effect — those bytes were
/// never debited.
/// </summary>
public sealed class PurgeOrphanedFilesJob(
    IServiceScopeFactory scopeFactory,
    IStorageService storage,
    ILogger<PurgeOrphanedFilesJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 600])]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // The recurring job is registered without a tenant, so none is resolved. Each tenant is purged
        // inside its own context, which also points FilesDbContext at a tenant's dedicated database.
        List<AppTenantInfo> tenants;
        using (var scope = scopeFactory.CreateScope())
        {
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
            tenants = (await tenantStore.GetAllAsync().ConfigureAwait(false)).ToList();
        }

        var now = DateTimeOffset.UtcNow;
        int total = 0;
        foreach (var tenant in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += await PurgeTenantAsync(tenant, now, cancellationToken).ConfigureAwait(false);
        }

        if (total > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Purged {Total} orphaned file assets across {TenantCount} tenants",
                total, tenants.Count);
        }
    }

    private async Task<int> PurgeTenantAsync(AppTenantInfo tenant, DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
            var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();

            // Only the soft-delete filter is lifted; the tenant filter stays on.
            var orphans = await db.FileAssets
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .Where(f => f.Status == FileAssetStatus.PendingUpload
                            && f.UploadDeadline != null
                            && f.UploadDeadline < now)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (orphans.Count == 0)
            {
                return 0;
            }

            foreach (var f in orphans)
            {
                try
                {
                    await storage.RemoveAsync(f.StorageKey, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to remove orphan storage object {Key} for tenant {TenantId}",
                        f.StorageKey, tenant.Id);
                }
            }

            // Hard delete (the row never reached Available, so soft-delete doesn't apply). FileAsset is
            // ISoftDeletable, so Remove() would become UPDATE IsDeleted=true — the bulk ExecuteDelete
            // bypasses the interceptor instead.
            var ids = orphans.Select(f => f.Id).ToList();
            await db.FileAssets
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .Where(f => ids.Contains(f.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Purged {Count} orphaned file assets for tenant {TenantId}",
                    orphans.Count, tenant.Id);
            }

            return orphans.Count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One tenant's database being unreachable must not keep the other tenants' orphans around.
            logger.LogError(ex, "Orphaned file purge failed for tenant {TenantId}", tenant.Id);
            return 0;
        }
    }
}
