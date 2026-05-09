using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories;

// SortBy: Sort column. One of: name | slug | createdAtUtc.
// SortDir: Sort direction. One of: asc | desc.
public sealed record SearchCategoriesQuery(
    string? Search = null,
    Guid? ParentCategoryId = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<CategoryDto>>;
