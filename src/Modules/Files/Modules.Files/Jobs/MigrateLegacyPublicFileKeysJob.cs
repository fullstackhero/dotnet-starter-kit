using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Files.Jobs;

/// <summary>
/// One-shot upgrade step for #1410. Bucket policies now grant anonymous read on <c>public/*</c> only,
/// and files uploaded before visibility lived in the storage key sit under <c>tenants/…</c>. This moves
/// every such <see cref="Visibility.Public"/> file to its <c>public/…</c> key (copy → update the row +
/// publish <c>FileStorageKeyChangedIntegrationEvent</c> → delete the old object, via
/// <see cref="FileStorageRelocator"/>) so public URLs keep working after the upgrade. Legacy private
/// files are left where they are: no policy grants anonymous read on them any more, which is the point.
/// <para>
/// Enqueued on every API start. It is idempotent — moved rows no longer match the query — so after the
/// first successful run it is a cheap no-op. Runs per tenant inside that tenant's Finbuckle context, which
/// also points <see cref="FilesDbContext"/> at a tenant's dedicated database when it has one.
/// </para>
/// </summary>
public sealed partial class MigrateLegacyPublicFileKeysJob(
    IServiceScopeFactory scopeFactory,
    ILogger<MigrateLegacyPublicFileKeysJob> logger)
{
    private const int BatchSize = 100;

    // A failed run is retried by the next API start rather than by Hangfire, so a startup that races the
    // migrator can't leave retries running alongside the next boot's run.
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        List<AppTenantInfo> tenants;
        using (var scope = scopeFactory.CreateScope())
        {
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
            tenants = (await tenantStore.GetAllAsync().ConfigureAwait(false)).ToList();
        }

        var total = 0;
        foreach (var tenant in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += await MigrateTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
        }

        if (total > 0)
        {
            LogMigrated(logger, total, tenants.Count);
        }
    }

    private async Task<int> MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        var moved = 0;
        try
        {
            // Tenant context goes on before anything resolves FilesDbContext: BaseDbContext captures the
            // tenant (and its connection string) at construction.
            using var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
            var relocator = scope.ServiceProvider.GetRequiredService<FileStorageRelocator>();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Soft-deleted files are included, since a restore must bring back a working public URL.
                // The tenant filter stays on. Pending uploads are left alone: their presigned PUT
                // targets the old key, and a later run picks them up once they are finalized.
                var batch = await db.FileAssets
                    .IgnoreQueryFilters([QueryFilters.SoftDelete])
                    .Where(f => f.Visibility == Visibility.Public
                        && f.Status == FileAssetStatus.Available
                        && !f.StorageKey.StartsWith(StorageVisibilityRoot.Public)
                        && !f.StorageKey.StartsWith(StorageVisibilityRoot.Private))
                    .OrderBy(f => f.Id)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (batch.Count == 0)
                {
                    break;
                }

                var movedInBatch = 0;
                foreach (var asset in batch)
                {
                    if (await TryRelocateAsync(db, relocator, asset, tenant.Id, cancellationToken).ConfigureAwait(false))
                    {
                        movedInBatch++;
                    }
                }

                moved += movedInBatch;
                if (movedInBatch == 0)
                {
                    // Every row in the batch failed; they would come back first again. Stop, and let
                    // the next run retry them.
                    break;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One tenant's database being unreachable must not keep the other tenants' files unmigrated.
            LogTenantFailed(logger, ex, tenant.Id);
        }

        return moved;
    }

    private async Task<bool> TryRelocateAsync(
        FilesDbContext db, FileStorageRelocator relocator, FileAsset asset, string? tenantId, CancellationToken cancellationToken)
    {
        try
        {
            await relocator.SaveAsync(asset, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFileFailed(logger, ex, asset.Id, asset.StorageKey, tenantId);
            // Drop the unsaved key change, or the next file's SaveChanges would commit it without its
            // event and without deleting the old object.
            db.Entry(asset).State = EntityState.Detached;
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Files] moved {Count} legacy public file(s) under public/ across {TenantCount} tenant(s)")]
    private static partial void LogMigrated(ILogger logger, int count, int tenantCount);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "[Files] legacy public-key migration failed for tenant {TenantId}; it is retried on the next API start")]
    private static partial void LogTenantFailed(ILogger logger, Exception exception, string? tenantId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "[Files] could not move legacy public file {FileAssetId} at {StorageKey} (tenant {TenantId}); it is retried on the next API start")]
    private static partial void LogFileFailed(ILogger logger, Exception exception, Guid fileAssetId, string storageKey, string? tenantId);
}
