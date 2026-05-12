namespace FSH.Modules.Catalog.Contracts.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    MoneyDto Price,
    int Stock,
    bool IsActive,
    // ThumbnailUrl — projection of the cover image's URL; null when the product has no images.
    // Derived server-side from the Images collection so list views don't need to ship Images.
    string? ThumbnailUrl,
    IReadOnlyList<ProductImageDto> Images,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);
