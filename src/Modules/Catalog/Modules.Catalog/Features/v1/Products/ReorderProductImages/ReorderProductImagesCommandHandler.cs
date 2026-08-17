using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products.ReorderProductImages;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.ReorderProductImages;

public sealed class ReorderProductImagesCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<ReorderProductImagesCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReorderProductImagesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.")
            {
                MessageKey = "Catalog.ProductNotFound",
                MessageArgs = [command.ProductId],
                ResourceSource = typeof(CatalogResources),
            };

        product.ReorderImages(command.OrderedImageIds);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
