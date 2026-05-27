namespace FSH.Modules.Catalog.Contracts.Dtos;

/// <summary>
/// A product image projection. <c>Url</c> is the durable, persisted public URL captured at attach
/// time from the Files module; <c>FileAssetId</c> is non-null when the image came in via the
/// presigned-upload flow (for cleanup bookkeeping) and null for legacy/external URLs.
/// </summary>
public sealed record ProductImageDto(
    Guid Id,
    Guid? FileAssetId,
    string Url,
    bool IsThumbnail,
    int SortOrder,
    DateTime CreatedAtUtc);
