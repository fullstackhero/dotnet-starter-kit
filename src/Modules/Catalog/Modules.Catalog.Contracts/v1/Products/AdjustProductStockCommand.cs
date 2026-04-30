using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products;

public sealed record AdjustProductStockCommand(
    Guid ProductId,
    int Delta) : ICommand<int>;
