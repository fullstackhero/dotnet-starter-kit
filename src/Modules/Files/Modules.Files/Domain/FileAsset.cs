using System.Net;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Domain.Events;

namespace FSH.Modules.Files.Domain;

/// <summary>
/// A file asset tracked by the Files module. Owns a presigned upload lifecycle (PendingUpload →
/// Available | Quarantined) plus soft-delete semantics consistent with Catalog/Tickets entities.
/// Tenant scoping is implicit (one DB/schema per tenant via the framework's BaseDbContext); we do
/// not carry a TenantId column here.
/// </summary>
public sealed class FileAsset : AggregateRoot<Guid>, ISoftDeletable
{
    public string OwnerType { get; private set; } = default!;
    public Guid? OwnerId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string OriginalFileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = default!;
    public Visibility Visibility { get; private set; }
    public FileAssetStatus Status { get; private set; }
    public ScanStatus ScanStatus { get; private set; }
    public DateTimeOffset? UploadDeadline { get; private set; }
    public string CreatedByUserId { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // ISoftDeletable — written by AuditableEntitySaveChangesInterceptor on dbContext.Remove(),
    // hidden from default queries by the global SoftDelete filter on BaseDbContext.
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private FileAsset() { }

    public static FileAsset CreatePending(
        Guid id,
        string ownerType,
        Guid? ownerId,
        string originalFileName,
        string sanitizedFileName,
        string contentType,
        long declaredSizeBytes,
        string storageKey,
        Visibility visibility,
        string createdByUserId,
        DateTimeOffset uploadDeadline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sanitizedFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        if (declaredSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(declaredSizeBytes), "Declared size must be positive.");
        }

        return new FileAsset
        {
            Id = id == Guid.Empty ? Guid.CreateVersion7() : id,
            OwnerType = ownerType,
            OwnerId = ownerId,
            OriginalFileName = originalFileName,
            FileName = sanitizedFileName,
            ContentType = contentType,
            SizeBytes = declaredSizeBytes,
            StorageKey = storageKey,
            Visibility = visibility,
            Status = FileAssetStatus.PendingUpload,
            ScanStatus = ScanStatus.NotScanned,
            UploadDeadline = uploadDeadline,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkAvailable(long actualSize, ScanStatus scanResult)
    {
        if (Status != FileAssetStatus.PendingUpload)
        {
            throw new CustomException(
                $"Cannot finalize file in status {Status}.",
                errors: null,
                HttpStatusCode.Conflict);
        }
        if (actualSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actualSize), "Actual size must be positive.");
        }

        SizeBytes = actualSize;
        ScanStatus = scanResult;
        Status = scanResult == ScanStatus.Infected ? FileAssetStatus.Quarantined : FileAssetStatus.Available;
        UploadDeadline = null;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new FileFinalizedDomainEvent(Id, OwnerType, OwnerId, Status, id, ts)));
    }

    /// <summary>Reverses a soft delete. Idempotent.</summary>
    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Flip the file's <see cref="Visibility"/> after upload. Idempotent. Refuses to mutate
    /// files that haven't finished uploading or are quarantined — those are not in a state
    /// where the URL contract is well-defined.
    /// </summary>
    public void ChangeVisibility(Visibility next)
    {
        if (Status != FileAssetStatus.Available)
        {
            throw new CustomException(
                $"Cannot change visibility while file is in status {Status}.",
                errors: null,
                HttpStatusCode.Conflict);
        }
        if (Visibility == next) return;
        Visibility = next;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
