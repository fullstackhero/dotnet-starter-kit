using System.Diagnostics;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Files.Services;

/// <summary>
/// Changes a <see cref="FileAsset"/> and, when its object is no longer under the storage root for its
/// <see cref="FileAsset.Visibility"/>, moves the object there. Bucket policies grant anonymous read on
/// <c>public/*</c> only, so the key has to follow the visibility.
/// <para>
/// <b>Serialization.</b> Everything happens while holding the file's row lock (<c>SELECT … FOR UPDATE</c>
/// on PostgreSQL) inside one transaction, and the row is re-read after the lock is taken. A migration
/// racing a user's visibility flip, or two flips racing each other, therefore run one after the other,
/// and each sees the other's result before deciding what to move.
/// </para>
/// <para>
/// <b>Move order.</b> Copy → commit (row + <see cref="FileStorageKeyChangedIntegrationEvent"/>) → delete the
/// old object. A copy left by a crashed attempt is reused. The old object is deleted only once no row
/// references it (<see cref="DeleteIfUnreferencedAsync"/>): first right after the commit, and again by
/// <c>DeleteSupersededObjectHandler</c> when the outbox delivers the event, which retries with backoff
/// until the object is really gone. A crash or a swallowed store error therefore leaves at most a
/// temporary orphan, never a row pointing at nothing and never a public copy of a private file for good.
/// </para>
/// </summary>
public sealed partial class FileStorageRelocator(
    FilesDbContext db,
    IStorageService storage,
    IOutboxWriter outbox,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ILogger<FileStorageRelocator> logger)
{
    /// <summary>
    /// Locks the file's row, re-reads it, applies <paramref name="mutate"/> and saves, relocating the
    /// object when the visibility root no longer matches. <paramref name="mutate"/> returns <c>false</c>
    /// to leave the row untouched (for example when the re-read row no longer needs the change).
    /// Returns the asset as re-read (and possibly changed), or <c>null</c> when it does not exist.
    /// </summary>
    public async Task<FileAsset?> ApplyAsync(
        Guid fileAssetId,
        Func<FileAsset, bool> mutate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        FileAsset? result = null;
        string? supersededKey = null;
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct =>
        {
            // A retried attempt starts from the database again, not from the failed attempt's state.
            db.ChangeTracker.Clear();
            result = null;
            supersededKey = null;

            await using var transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            var asset = await LoadLockedAsync(fileAssetId, ct).ConfigureAwait(false);
            if (asset is null || !mutate(asset))
            {
                result = asset;
                return;
            }

            var oldKey = asset.StorageKey;
            var newKey = StorageKeyBuilder.ForVisibility(oldKey, asset.Visibility);
            var moves = !string.Equals(oldKey, newKey, StringComparison.Ordinal);
            if (moves)
            {
                // Skip the copy only on a confirmed hit: ExistsAsync reports false on store errors, and the
                // copy itself throws on a missing source or a store failure, which aborts before the row moves.
                if (!await storage.ExistsAsync(newKey, ct).ConfigureAwait(false))
                {
                    await storage.CopyAsync(oldKey, newKey, ct).ConfigureAwait(false);
                }

                asset.RelocateStorage(newKey);
            }

            // acceptAllChangesOnSuccess: false keeps the tracked changes if the commit fails and the
            // execution strategy retries; they are accepted once the commit has gone through.
            await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, ct).ConfigureAwait(false);
            if (moves)
            {
                await outbox.AddAsync(NewEvent(asset, oldKey, newKey), ct).ConfigureAwait(false);
            }

            await transaction.CommitAsync(ct).ConfigureAwait(false);
            db.ChangeTracker.AcceptAllChanges();

            result = asset;
            supersededKey = moves ? oldKey : null;
        }, cancellationToken).ConfigureAwait(false);

        if (supersededKey is not null && result is not null)
        {
            LogRelocated(logger, result.Id, supersededKey, result.StorageKey);

            // First attempt at removing the old object. Not tied to the caller's token: the row already
            // points elsewhere, so an aborted request must not leave the old (possibly public) copy behind.
            // The outbox handler retries this until it sticks.
            try
            {
                await DeleteIfUnreferencedAsync(result.Id, supersededKey, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogDeleteDeferred(logger, ex, result.Id, supersededKey);
            }
        }

        return result;
    }

    /// <summary>
    /// Deletes the object at <paramref name="storageKey"/> once no FileAsset row references it, holding
    /// <paramref name="fileAssetId"/>'s row lock so a concurrent move of that file cannot copy into the key
    /// while it is being deleted. Throws when the object is still there afterwards, so callers (the outbox)
    /// retry. A key some row still references is left alone and counts as done.
    /// </summary>
    public async Task DeleteIfUnreferencedAsync(Guid fileAssetId, string storageKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            _ = await LoadLockedAsync(fileAssetId, ct).ConfigureAwait(false);

            var referenced = await db.FileAssets
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .AnyAsync(f => f.StorageKey == storageKey, ct)
                .ConfigureAwait(false);
            if (!referenced)
            {
                await storage.RemoveAsync(storageKey, ct).ConfigureAwait(false);

                // RemoveAsync swallows store errors, so check the delete actually took.
                if (await storage.ExistsAsync(storageKey, ct).ConfigureAwait(false))
                {
                    throw new InvalidOperationException(
                        $"Superseded object '{storageKey}' of file {fileAssetId} is still present after delete.");
                }
            }

            await transaction.CommitAsync(ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    // Row lock + fresh read. EF identity resolution would otherwise hand back an already-tracked instance
    // with stale values, hence the ChangeTracker.Clear() at the start of each attempt.
    private Task<FileAsset?> LoadLockedAsync(Guid fileAssetId, CancellationToken ct)
    {
        var query = db.Database.IsNpgsql()
            ? db.FileAssets.FromSqlRaw(
                $$"""SELECT * FROM "{{FilesDbContext.Schema}}"."FileAssets" WHERE "Id" = {0} FOR UPDATE""",
                fileAssetId)
            : db.FileAssets.Where(f => f.Id == fileAssetId); // no portable row lock; single-writer only

        return query
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(ct);
    }

    private FileStorageKeyChangedIntegrationEvent NewEvent(FileAsset asset, string oldKey, string newKey) => new(
        Id: Guid.NewGuid(),
        OccurredOnUtc: DateTime.UtcNow,
        TenantId: tenantAccessor.MultiTenantContext?.TenantInfo?.Id,
        CorrelationId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
        Source: "Files",
        FileAssetId: asset.Id,
        OwnerType: asset.OwnerType,
        OwnerId: asset.OwnerId,
        OldStorageKey: oldKey,
        NewStorageKey: newKey,
        Visibility: asset.Visibility,
        // The API's current public URL for the new key, so consumers don't keep an outdated base
        // (e.g. a compose URL built before PublicBaseUrl was set).
        NewPublicUrl: asset.Visibility == Visibility.Public ? storage.BuildPublicUrl(newKey) : null);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Files] moved file {FileAssetId} from {OldStorageKey} to {NewStorageKey}")]
    private static partial void LogRelocated(ILogger logger, Guid fileAssetId, string oldStorageKey, string newStorageKey);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Files] could not delete the superseded object {StorageKey} of file {FileAssetId} yet; the outbox retries it")]
    private static partial void LogDeleteDeferred(ILogger logger, Exception exception, Guid fileAssetId, string storageKey);
}
