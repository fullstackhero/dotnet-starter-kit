using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands;

public sealed record SearchBrandsQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20,
    /// <summary>Sort column. One of: name | slug | createdAtUtc.</summary>
    string? SortBy = null,
    /// <summary>Sort direction. One of: asc | desc.</summary>
    string? SortDir = null) : IQuery<PagedResponse<BrandDto>>;
