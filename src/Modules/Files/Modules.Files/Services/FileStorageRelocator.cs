using System.Diagnostics;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Files.Services;

/// <summary>
/// Saves a <see cref="FileAsset"/> and, when its object is not under the storage root for its
/// <see cref="FileAsset.Visibility"/>, moves the object there first. Bucket policies grant anonymous
/// read on <c>public/*</c> only, so the key has to follow the visibility.
/// <para>
/// The move is copy → commit (row + <see cref="FileStorageKeyChangedIntegrationEvent"/>, one
/// transaction) → delete the old object. Every step is safe to repeat: a copy that already landed is
/// reused, and the old object is only deleted after the row points at the new one, so a crash at any
/// point leaves at worst an orphaned copy, never a row pointing at nothing.
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
    /// Persists pending changes on <paramref name="asset"/>, relocating its object first when needed.
    /// Returns <c>true</c> when the object moved.
    /// </summary>
    public async Task<bool> SaveAsync(FileAsset asset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var oldKey = asset.StorageKey;
        var newKey = StorageKeyBuilder.ForVisibility(oldKey, asset.Visibility);
        if (string.Equals(oldKey, newKey, StringComparison.Ordinal))
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        // 1. Copy, unless a previous attempt that crashed after its copy already put the object there.
        // ExistsAsync reports false on any store error, so only a positive answer skips the copy; the copy
        // itself is authoritative and throws when the source is missing or the store fails, which stops
        // the move before the row is repointed.
        if (!await storage.ExistsAsync(newKey, cancellationToken).ConfigureAwait(false))
        {
            await storage.CopyAsync(oldKey, newKey, cancellationToken).ConfigureAwait(false);
        }

        // 2. Commit the new key together with the event that tells owning modules to rewrite their URLs.
        asset.RelocateStorage(newKey);
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await outbox.AddAsync(new FileStorageKeyChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: DateTime.UtcNow,
                TenantId: tenantId,
                CorrelationId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                Source: "Files",
                FileAssetId: asset.Id,
                OwnerType: asset.OwnerType,
                OwnerId: asset.OwnerId,
                OldStorageKey: oldKey,
                NewStorageKey: newKey,
                Visibility: asset.Visibility), ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        // 3. Only now is the old object unreferenced. RemoveAsync is best-effort: a failure leaves an
        // orphaned object behind, never a broken reference.
        await storage.RemoveAsync(oldKey, cancellationToken).ConfigureAwait(false);

        LogRelocated(logger, asset.Id, oldKey, newKey);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Files] moved file {FileAssetId} from {OldStorageKey} to {NewStorageKey}")]
    private static partial void LogRelocated(ILogger logger, Guid fileAssetId, string oldStorageKey, string newStorageKey);
}
