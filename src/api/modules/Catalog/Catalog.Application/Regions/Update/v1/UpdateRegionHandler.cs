using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Update.v1;

public sealed class UpdateRegionHandler(
    ILogger<UpdateRegionHandler> logger,
    IRepository<Region> repository)
    : IRequestHandler<UpdateRegionCommand, UpdateRegionResponse>
{
    public async Task<UpdateRegionResponse> Handle(UpdateRegionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var region = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = region ?? throw new RegionNotFoundException(request.Id);
        var updatedRegion = region.Update(request.Name, request.Description);
        await repository.UpdateAsync(updatedRegion, cancellationToken);
        logger.LogInformation("Region with id : {RegionId} updated.", region.Id);
        return new UpdateRegionResponse(region.Id);
    }
}
