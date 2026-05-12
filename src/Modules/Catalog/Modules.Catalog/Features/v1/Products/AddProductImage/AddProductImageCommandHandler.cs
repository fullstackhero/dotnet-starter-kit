using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Contracts.v1.Products.AddProductImage;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.AddProductImage;

public sealed class AddProductImageCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<AddProductImageCommand, ProductImageDto>
{
    public async ValueTask<ProductImageDto> Handle(AddProductImageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        var image = product.AddImage(command.FileAssetId, command.Url);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ProductImageDto(image.Id, image.FileAssetId, image.Url, image.IsThumbnail, image.SortOrder, image.CreatedAtUtc);
    }
}
