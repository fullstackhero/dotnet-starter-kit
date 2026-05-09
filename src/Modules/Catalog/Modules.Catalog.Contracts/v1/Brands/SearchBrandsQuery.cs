using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands;

/// <summary>
/// Search for brands with pagination and sorting.
/// </summary>
/// <param name="Search">Search term.</param>
/// <param name="PageNumber">Page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="SortBy">Sort column. One of: name | slug | createdAtUtc.</param>
/// <param name="SortDir">Sort direction. One of: asc | desc.</param>
public sealed record SearchBrandsQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<BrandDto>>;
