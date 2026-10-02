using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Jobs;

/// <summary>
/// Daily purge of soft-deleted FileAsset rows past the retention window, in every tenant. Hard-deletes
/// the row, removes the bytes from storage, and refunds the owning tenant's quota (the bytes were
/// debited at finalize time).
/// </summary>
public sealed class PurgeDeletedFilesJob(
    IServiceScopeFactory scopeFactory,
    IStorageService storage,
    IQuotaService quotas,
    IOptions<FilesOptions> options,
    ILogger<PurgeDeletedFilesJob> logger)
{
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [300, 1800])]
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

        var cutoff = DateTimeOffset.UtcNow.AddDays(-options.Value.SoftDeleteRetentionDays);
        int total = 0;
        foreach (var tenant in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += await PurgeTenantAsync(tenant, cutoff, cancellationToken).ConfigureAwait(false);
        }

        if (total > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Hard-purged {Total} soft-deleted file assets across {TenantCount} tenants",
                total, tenants.Count);
        }
    }

    private async Task<int> PurgeTenantAsync(AppTenantInfo tenant, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
            var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();

            // Soft-deleted rows are hidden by the soft-delete filter; lift only that one so the tenant
            // filter stays on.
            var candidates = await db.FileAssets
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .Where(f => f.IsDeleted && f.DeletedOnUtc != null && f.DeletedOnUtc < cutoff)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (candidates.Count == 0)
            {
                return 0;
            }

            // Best-effort byte removal per file.
            foreach (var f in candidates)
            {
                try
                {
                    await storage.RemoveAsync(f.StorageKey, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Storage remove failed for {Key} for tenant {TenantId}",
                        f.StorageKey, tenant.Id);
                }
            }

            var ids = candidates.Select(f => f.Id).ToList();
            await db.FileAssets
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .Where(f => ids.Contains(f.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Refund the bytes debited at finalize, under the tenant that owns them. Done after the
            // rows are gone so a failed delete can't refund bytes that are still counted as stored.
            var totalBytes = candidates.Sum(f => f.SizeBytes);
            if (totalBytes > 0)
            {
                try
                {
                    await quotas.RecordAsync(tenant.Id, QuotaResource.StorageBytes, -totalBytes, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Quota refund of {Bytes} bytes failed for tenant {TenantId}",
                        totalBytes, tenant.Id);
                }
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Hard-purged {Count} soft-deleted file assets ({Bytes} bytes) for tenant {TenantId}",
                    candidates.Count, totalBytes, tenant.Id);
            }

            return candidates.Count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One tenant's database being unreachable must not keep the other tenants' files around.
            logger.LogError(ex, "Deleted file purge failed for tenant {TenantId}", tenant.Id);
            return 0;
        }
    }
}
