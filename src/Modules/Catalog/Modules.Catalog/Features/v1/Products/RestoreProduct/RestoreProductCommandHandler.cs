using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.RestoreProduct;

public sealed class RestoreProductCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<RestoreProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestoreProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await dbContext.Products
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        product.Restore();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return product.Id;
    }
}
