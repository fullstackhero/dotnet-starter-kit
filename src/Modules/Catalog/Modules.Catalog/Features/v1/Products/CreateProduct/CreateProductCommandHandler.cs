using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.CreateProduct;

public sealed class CreateProductCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool brandExists = await dbContext.Brands
            .AnyAsync(b => b.Id == command.BrandId, cancellationToken)
            .ConfigureAwait(false);
        if (!brandExists)
        {
            throw new NotFoundException($"Brand {command.BrandId} not found.");
        }

        bool categoryExists = await dbContext.Categories
            .AnyAsync(c => c.Id == command.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (!categoryExists)
        {
            throw new NotFoundException($"Category {command.CategoryId} not found.");
        }

        var product = Product.Create(
            command.Sku,
            command.Name,
            command.Description,
            command.BrandId,
            command.CategoryId,
            new Money(command.PriceAmount, command.PriceCurrency),
            command.Stock,
            command.ImageUrl);

        bool skuTaken = await dbContext.Products
            .AnyAsync(p => p.Sku == product.Sku, cancellationToken)
            .ConfigureAwait(false);
        if (skuTaken)
        {
            throw new CustomException(
                $"A product with SKU '{product.Sku}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        bool slugTaken = await dbContext.Products
            .AnyAsync(p => p.Slug == product.Slug, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"A product with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return product.Id;
    }
}
