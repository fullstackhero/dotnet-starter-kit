using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.CreateBrand;

public sealed class CreateBrandCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<CreateBrandCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var brand = Brand.Create(command.Name, command.Description, command.LogoUrl);

        bool slugTaken = await dbContext.Brands
            .AnyAsync(b => b.Slug == brand.Slug, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"A brand with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return brand.Id;
    }
}
