using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description = null,
    Guid? ParentCategoryId = null) : ICommand<Guid>;
