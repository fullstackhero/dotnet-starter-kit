using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    bool IsActive) : ICommand<Guid>;
