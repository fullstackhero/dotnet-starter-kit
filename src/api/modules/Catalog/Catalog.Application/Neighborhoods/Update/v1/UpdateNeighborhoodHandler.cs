using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Update.v1;
public sealed class UpdateNeighborhoodHandler(
    ILogger<UpdateNeighborhoodHandler> logger,
    [FromKeyedServices("catalog:neighborhoods")] IRepository<Neighborhood> repository)
    : IRequestHandler<UpdateNeighborhoodCommand, UpdateNeighborhoodResponse>
{
    public async Task<UpdateNeighborhoodResponse> Handle(UpdateNeighborhoodCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var Neighborhood = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = Neighborhood ?? throw new NeighborhoodNotFoundException(request.Id);
        var updatedNeighborhood = Neighborhood.Update(request.Name, request.Description, request.CityId, request.SphereImgURL, request.Score);
        await repository.UpdateAsync(updatedNeighborhood, cancellationToken);
        logger.LogInformation("Neighborhood with id : {NeighborhoodId} updated.", Neighborhood.Id);
        return new UpdateNeighborhoodResponse(Neighborhood.Id);
    }
}
