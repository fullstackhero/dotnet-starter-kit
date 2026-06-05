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

        // IgnoreAutoIncludes is load-bearing: if Product.Images (AutoInclude'd) load here, Remove() cascades Deleted onto them
        // and the soft-delete interceptor (rescues only owned refs) HARD-deletes them. Untracked keeps the delete a pure UPDATE so rows survive restore.
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
