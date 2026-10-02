using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products.RemoveProductImage;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.RemoveProductImage;

public sealed class RemoveProductImageCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<RemoveProductImageCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveProductImageCommand command, CancellationToken cancellationToken)
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

        // Domain throws InvalidOperationException for unknown imageId; translate to 404.
        if (!product.Images.Any(i => i.Id == command.ImageId))
        {
            throw new NotFoundException($"Image {command.ImageId} not found on product {command.ProductId}.")
            {
                MessageKey = "Catalog.ProductImageNotFound",
                MessageArgs = [command.ImageId, command.ProductId],
                ResourceSource = typeof(CatalogResources),
            };
        }

        product.RemoveImage(command.ImageId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
