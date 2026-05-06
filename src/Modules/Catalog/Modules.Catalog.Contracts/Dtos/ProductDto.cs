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
    string? ImageUrl,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);
