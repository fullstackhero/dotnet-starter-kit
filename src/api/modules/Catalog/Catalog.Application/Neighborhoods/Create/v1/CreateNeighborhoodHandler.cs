using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Create.v1;

public sealed class CreateNeighborhoodHandler(
    ILogger<CreateNeighborhoodHandler> logger,
    IRepository<Neighborhood> repository)
    : IRequestHandler<CreateNeighborhoodCommand, CreateNeighborhoodResponse>
{
    public async Task<CreateNeighborhoodResponse> Handle(CreateNeighborhoodCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var neighborhood = Neighborhood.Create(request.Name, request.Description, request.CityId, request.SphereImgURL, request.Score);
        await repository.AddAsync(neighborhood, cancellationToken);
        logger.LogInformation("Neighborhood created {NeighborhoodId}", neighborhood.Id);
        return new CreateNeighborhoodResponse(neighborhood.Id, neighborhood.Name, neighborhood.Description, neighborhood.CityId, neighborhood.SphereImgURL, neighborhood.Score);
    }
}
