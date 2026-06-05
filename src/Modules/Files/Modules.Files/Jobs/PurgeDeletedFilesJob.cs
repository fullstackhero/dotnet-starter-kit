using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Jobs;

/// <summary>
/// Daily purge of soft-deleted FileAsset rows past the retention window. Hard-deletes the row,
/// removes the bytes from storage, and refunds the quota (the bytes were debited at finalize time).
/// </summary>
public sealed class PurgeDeletedFilesJob(
    FilesDbContext db,
    IStorageService storage,
    IQuotaService quotas,
    IOptions<FilesOptions> options,
    ILogger<PurgeDeletedFilesJob> logger)
{
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [300, 1800])]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-options.Value.SoftDeleteRetentionDays);
        var candidates = await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.IsDeleted && f.DeletedOnUtc != null && f.DeletedOnUtc < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidates.Count == 0)
        {
            return;
        }

        // Best-effort byte removal per file. Schema-per-tenant means all rows share one tenant,
        // and Hangfire wires the job per-tenant for multi-tenant deployments.
        foreach (var f in candidates)
        {
            try
            {
                await storage.RemoveAsync(f.StorageKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Storage remove failed for {Key}", f.StorageKey);
            }
        }

        // Quota refund — group bytes once per tenant. In schema-per-tenant the resolved tenant
        // matches every row's logical tenant; the framework's QuotaService is tenant-scoped via DI.
        var totalBytes = candidates.Sum(f => f.SizeBytes);
        if (totalBytes > 0)
        {
            // Empty tenant id satisfies the contract; QuotaService resolves the tenant from DI.
            // Falls back gracefully with no tenant (the refund is simply lost).
            try
            {
                await quotas.RecordAsync("", QuotaResource.StorageBytes, -totalBytes, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Quota refund failed for {Bytes} bytes", totalBytes);
            }
        }

        var ids = candidates.Select(f => f.Id).ToList();
        await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => ids.Contains(f.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Hard-purged {Count} soft-deleted file assets ({Bytes} bytes total)",
                candidates.Count, totalBytes);
        }
    }
}
