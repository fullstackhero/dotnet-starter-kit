using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.DeleteBrand;

public sealed class DeleteBrandCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<DeleteBrandCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(b => b.Id == command.BrandId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Brand {command.BrandId} not found.")
            {
                MessageKey = "Catalog.BrandNotFound",
                MessageArgs = [command.BrandId],
                ResourceSource = typeof(CatalogResources),
            };

        dbContext.Brands.Remove(brand);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
