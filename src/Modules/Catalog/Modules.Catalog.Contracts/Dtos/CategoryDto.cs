namespace FSH.Modules.Catalog.Contracts.Dtos;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);
