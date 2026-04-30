namespace FSH.Modules.Catalog.Contracts.Dtos;

public sealed record CategoryTreeNodeDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyList<CategoryTreeNodeDto> Children);
