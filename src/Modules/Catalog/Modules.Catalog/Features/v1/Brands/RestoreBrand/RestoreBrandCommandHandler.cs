using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.RestoreBrand;

public sealed class RestoreBrandCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<RestoreBrandCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestoreBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // IgnoreQueryFilters lets us load soft-deleted rows; the global
        // filter set on BaseDbContext would otherwise hide them.
        var brand = await dbContext.Brands
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == command.BrandId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Brand {command.BrandId} not found.");

        brand.Restore();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return brand.Id;
    }
}
