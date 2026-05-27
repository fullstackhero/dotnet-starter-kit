using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProductById;

public sealed class GetProductByIdQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    public async ValueTask<ProductDto> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.ProductId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {query.ProductId} not found.");

        return product.ToDto();
    }
}
