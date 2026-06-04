using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Files.Jobs;

/// <summary>
/// Hourly purge of FileAsset rows stuck in PendingUpload past their UploadDeadline. Best-effort
/// removal of any bytes that did make it to storage. No quota effect — those bytes were never
/// debited.
/// </summary>
public sealed class PurgeOrphanedFilesJob(
    FilesDbContext db,
    IStorageService storage,
    ILogger<PurgeOrphanedFilesJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 600])]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var orphans = await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.Status == FileAssetStatus.PendingUpload
                        && f.UploadDeadline != null
                        && f.UploadDeadline < now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (orphans.Count == 0)
        {
            return;
        }

        foreach (var f in orphans)
        {
            try
            {
                await storage.RemoveAsync(f.StorageKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to remove orphan storage object {Key}", f.StorageKey);
            }
            // Hard delete — the row never made it to Available, so soft-delete doesn't apply.
            // We need EF to issue a DELETE, but FileAsset is ISoftDeletable and the interceptor
            // would convert Remove() to UPDATE IsDeleted=true. Work around by setting IsDeleted
            // already (forces interceptor to skip) and calling Remove again. Cleaner path: an
            // explicit ExecuteDelete bulk operation.
        }

        // Bulk hard delete — bypasses the soft-delete interceptor.
        var ids = orphans.Select(f => f.Id).ToList();
        await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => ids.Contains(f.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Purged {Count} orphaned file assets", orphans.Count);
        }
    }
}
