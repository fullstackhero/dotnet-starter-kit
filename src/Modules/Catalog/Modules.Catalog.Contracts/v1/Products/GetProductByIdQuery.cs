using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;
