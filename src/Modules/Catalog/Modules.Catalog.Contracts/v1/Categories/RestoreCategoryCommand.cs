using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories;

public sealed record RestoreCategoryCommand(Guid CategoryId) : ICommand<Guid>;
