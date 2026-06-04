using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<DeleteProductCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // IgnoreAutoIncludes is load-bearing: Product.Images is AutoInclude'd and cascade-deleted.
        // If we let the images load here, Remove() cascades EntityState.Deleted onto them, and the
        // soft-delete interceptor only rescues owned references (Price/Money) — not this non-owned
        // child collection — so EF would HARD-delete the image rows while the product is merely
        // soft-deleted. Restoring the product would then come back with no images. Leaving the images
        // untracked means the soft delete is a pure UPDATE and the image rows survive for restore.
        var product = await dbContext.Products
            .IgnoreAutoIncludes()
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
