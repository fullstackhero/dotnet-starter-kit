using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Create.v1;

public sealed class CreateRegionHandler(
    ILogger<CreateRegionHandler> logger,
    IRepository<Region> repository)
    : IRequestHandler<CreateRegionCommand, CreateRegionResponse>
{
    public async Task<CreateRegionResponse> Handle(CreateRegionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var region = Region.Create(request.Name, request.Description);
        await repository.AddAsync(region, cancellationToken);
        logger.LogInformation("Region created {RegionId}", region.Id);
        return new CreateRegionResponse(region.Id, region.Name, region.Description);
    }
}