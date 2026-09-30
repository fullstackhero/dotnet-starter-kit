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
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Jobs;

/// <summary>
/// Upgrade step for #1410. Bucket policies now grant anonymous read on <c>public/*</c> only, and files
/// uploaded before visibility lived in the storage key sit under <c>tenants/…</c>. This moves every such
/// <see cref="Visibility.Public"/>, Available file to its <c>public/…</c> key through
/// <see cref="FileStorageRelocator"/> (row-locked copy → commit row + event → delete old object), so public
/// URLs keep working after the upgrade. Legacy private files stay where they are: no policy grants
/// anonymous read on them any more, which is the point.
/// <para>
/// <b>Cost when there is nothing to do.</b> The scan reads <c>IX_FileAsset_LegacyKey</c>, a partial index over
/// rows whose key has no visibility root. New keys always have one, so once a tenant is migrated the index
/// is empty and each start costs one index probe per tenant database — no completion marker to keep in
/// sync, and new tenants are "done" from the start. Files finalized with a legacy key after the upgrade
/// (presigned before it) are relocated by <c>FinalizeUpload</c> itself, and would otherwise still show up here.
/// </para>
/// <para>
/// <b>Progress past failures.</b> Rows are read in id order with keyset pagination, one DI scope per page
/// (so change trackers don't grow), and a row that fails — e.g. its object is missing — is logged and
/// skipped rather than re-read. If anything failed the run throws at the end, so Hangfire retries it with
/// backoff; per-file row locks make an overlapping or repeated run safe.
/// </para>
/// </summary>
public sealed partial class MigrateLegacyPublicFileKeysJob(
    IServiceScopeFactory scopeFactory,
    IOptions<FilesOptions> options,
    ILogger<MigrateLegacyPublicFileKeysJob> logger)
{
    private readonly int _batchSize = Math.Max(1, options.Value.LegacyKeyMigrationBatchSize);

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 1800])]
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        List<AppTenantInfo> tenants;
        using (var scope = scopeFactory.CreateScope())
        {
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
            tenants = (await tenantStore.GetAllAsync().ConfigureAwait(false)).ToList();
        }

        var moved = 0;
        var failed = 0;
        foreach (var tenant in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (tenantMoved, tenantFailed) = await MigrateTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
            moved += tenantMoved;
            failed += tenantFailed;
        }

        if (moved > 0)
        {
            LogMigrated(logger, moved, tenants.Count);
        }

        if (failed > 0)
        {
            // Surfaces to Hangfire so it retries with backoff; everything that did move stays moved.
            throw new InvalidOperationException(
                $"Legacy public-key migration left {failed} file(s) or tenant(s) unmigrated; see the preceding errors.");
        }
    }

    private async Task<(int Moved, int Failed)> MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        var moved = 0;
        var failed = 0;
        Guid? lastId = null;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // One scope per page. The tenant context goes on before anything resolves FilesDbContext:
                // BaseDbContext captures the tenant (and its connection string) at construction.
                using var scope = scopeFactory.CreateScope();
                scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                    .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
                var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
                var relocator = scope.ServiceProvider.GetRequiredService<FileStorageRelocator>();

                var page = await LegacyPublicIdsAsync(db, lastId, cancellationToken).ConfigureAwait(false);
                if (page.Count == 0)
                {
                    break;
                }

                foreach (var id in page)
                {
                    if (await TryRelocateAsync(relocator, id, tenant.Id, cancellationToken).ConfigureAwait(false))
                    {
                        moved++;
                    }
                    else
                    {
                        failed++;
                    }
                }

                // Keyset: the next page starts after this one, so failed rows don't come back first.
                lastId = page[^1];
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One tenant's database being unreachable must not keep the other tenants' files unmigrated.
            LogTenantFailed(logger, ex, tenant.Id);
            failed++;
        }

        return (moved, failed);
    }

    // The key predicate matches the filter of IX_FileAsset_LegacyKey word for word, so PostgreSQL can
    // answer this from that (normally empty) partial index. Soft-deleted files are included, since a
    // restore must bring back a working public URL; the tenant filter stays on. Pending uploads are left
    // to FinalizeUpload, which relocates them when they complete.
    private Task<List<Guid>> LegacyPublicIdsAsync(FilesDbContext db, Guid? lastId, CancellationToken ct)
    {
        var query = db.FileAssets
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .AsNoTracking()
            .Where(f => !EF.Functions.Like(f.StorageKey, StorageVisibilityRoot.Public + "%")
                && !EF.Functions.Like(f.StorageKey, StorageVisibilityRoot.Private + "%")
                && f.Visibility == Visibility.Public
                && f.Status == FileAssetStatus.Available);

        if (lastId is { } after)
        {
            query = query.Where(f => f.Id > after);
        }

        return query
            .OrderBy(f => f.Id)
            .Select(f => f.Id)
            .Take(_batchSize)
            .ToListAsync(ct);
    }

    private async Task<bool> TryRelocateAsync(
        FileStorageRelocator relocator, Guid fileAssetId, string? tenantId, CancellationToken cancellationToken)
    {
        try
        {
            // Re-checked under the row lock: a concurrent visibility flip or another run may have got there
            // first, in which case the file is left as that writer left it.
            await relocator.ApplyAsync(
                fileAssetId,
                asset => asset.Visibility == Visibility.Public
                    && asset.Status == FileAssetStatus.Available
                    && !StorageKeyBuilder.IsUnderVisibilityRoot(asset.StorageKey, Visibility.Public)
                    && !StorageKeyBuilder.IsUnderVisibilityRoot(asset.StorageKey, Visibility.Private),
                cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFileFailed(logger, ex, fileAssetId, tenantId);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Files] moved {Count} legacy public file(s) under public/ across {TenantCount} tenant(s)")]
    private static partial void LogMigrated(ILogger logger, int count, int tenantCount);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "[Files] legacy public-key migration failed for tenant {TenantId}; the job is retried")]
    private static partial void LogTenantFailed(ILogger logger, Exception exception, string? tenantId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "[Files] could not move legacy public file {FileAssetId} (tenant {TenantId}); the job is retried")]
    private static partial void LogFileFailed(ILogger logger, Exception exception, Guid fileAssetId, string? tenantId);
}
