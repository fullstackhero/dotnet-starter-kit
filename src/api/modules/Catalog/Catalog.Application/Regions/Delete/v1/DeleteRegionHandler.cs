using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Delete.v1;

public sealed class DeleteRegionHandler(
    ILogger<DeleteRegionHandler> logger,
    [FromKeyedServices("catalog:regions")] IRepository<Region> repository)
    : IRequestHandler<DeleteRegionCommand>
{

    public async Task Handle(DeleteRegionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var region = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = region ?? throw new RegionNotFoundException(request.Id);
        await repository.DeleteAsync(region, cancellationToken);
        logger.LogInformation("Region deleted {RegionId}", region.Id);
    }

}
