using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products;

public sealed record ChangeProductPriceCommand(
    Guid ProductId,
    decimal Amount,
    string Currency) : ICommand<Guid>;
