using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories;

public sealed record ListTrashedCategoriesQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResponse<CategoryDto>>;
