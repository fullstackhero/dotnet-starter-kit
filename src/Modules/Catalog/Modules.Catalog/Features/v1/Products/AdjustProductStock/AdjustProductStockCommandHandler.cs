using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.AdjustProductStock;

public sealed class AdjustProductStockCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<AdjustProductStockCommand, int>
{
    public async ValueTask<int> Handle(AdjustProductStockCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.")
                .Localized<CatalogResources>("Catalog.ProductNotFound", command.ProductId);

        try
        {
            product.AdjustStock(command.Delta);
        }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, (IEnumerable<string>?)null, HttpStatusCode.Conflict)
                .Localized<CatalogResources>("Catalog.StockAdjustmentNegative", command.Delta, product.Stock);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return product.Stock;
    }
}
