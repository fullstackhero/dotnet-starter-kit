using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories;

public sealed record GetCategoryTreeQuery : IQuery<IReadOnlyList<CategoryTreeNodeDto>>;
