using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products;

public sealed record RestoreProductCommand(Guid ProductId) : ICommand<Guid>;
