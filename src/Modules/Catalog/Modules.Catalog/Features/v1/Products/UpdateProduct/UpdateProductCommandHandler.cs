using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UpdateProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        if (product.BrandId != command.BrandId)
        {
            bool brandExists = await dbContext.Brands
                .AnyAsync(b => b.Id == command.BrandId, cancellationToken)
                .ConfigureAwait(false);
            if (!brandExists)
            {
                throw new NotFoundException($"Brand {command.BrandId} not found.");
            }
        }

        if (product.CategoryId != command.CategoryId)
        {
            bool categoryExists = await dbContext.Categories
                .AnyAsync(c => c.Id == command.CategoryId, cancellationToken)
                .ConfigureAwait(false);
            if (!categoryExists)
            {
                throw new NotFoundException($"Category {command.CategoryId} not found.");
            }
        }

        product.Update(
            command.Name,
            command.Description,
            command.BrandId,
            command.CategoryId,
            command.ImageUrl,
            command.IsActive);

        bool slugTaken = await dbContext.Products
            .AnyAsync(p => p.Slug == product.Slug && p.Id != product.Id, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"Another product with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return product.Id;
    }
}
