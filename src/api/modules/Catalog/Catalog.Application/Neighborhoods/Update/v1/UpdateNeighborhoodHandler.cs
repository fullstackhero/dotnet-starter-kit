using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Update.v1;

public sealed class UpdateNeighborhoodHandler(
    ILogger<UpdateNeighborhoodHandler> logger,
    IRepository<Neighborhood> repository)
    : IRequestHandler<UpdateNeighborhoodCommand, UpdateNeighborhoodResponse>
{
    public async Task<UpdateNeighborhoodResponse> Handle(UpdateNeighborhoodCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var neighborhood = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = neighborhood ?? throw new NeighborhoodNotFoundException(request.Id);
        var updatedNeighborhood = neighborhood.Update(request.Name, request.Description, request.CityId, request.SphereImgURL, request.Score);
        await repository.UpdateAsync(updatedNeighborhood, cancellationToken);
        logger.LogInformation("Neighborhood with id : {NeighborhoodId} updated.", neighborhood.Id);
        return new UpdateNeighborhoodResponse(neighborhood.Id);
    }
}