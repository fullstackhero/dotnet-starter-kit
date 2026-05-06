using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories;

public sealed record SearchCategoriesQuery(
    string? Search = null,
    Guid? ParentCategoryId = null,
    int PageNumber = 1,
    int PageSize = 50,
    /// <summary>Sort column. One of: name | slug | createdAtUtc.</summary>
    string? SortBy = null,
    /// <summary>Sort direction. One of: asc | desc.</summary>
    string? SortDir = null) : IQuery<PagedResponse<CategoryDto>>;
