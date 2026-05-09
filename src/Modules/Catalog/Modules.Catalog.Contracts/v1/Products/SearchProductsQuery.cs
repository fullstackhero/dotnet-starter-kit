using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products;

// SortBy: Sort column. One of: name | sku | createdAtUtc | stock | price.
// SortDir: Sort direction. One of: asc | desc.
public sealed record SearchProductsQuery(
    string? Search = null,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<ProductDto>>;
