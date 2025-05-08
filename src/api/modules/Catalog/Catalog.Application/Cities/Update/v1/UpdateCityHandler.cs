using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Update.v1;
public sealed class UpdateCityHandler(
    ILogger<UpdateCityHandler> logger,
    [FromKeyedServices("catalog:cities")] IRepository<City> repository)
    : IRequestHandler<UpdateCityCommand, UpdateCityResponse>
{
    public async Task<UpdateCityResponse> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var city = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = city ?? throw new CityNotFoundException(request.Id);
        var updatedCity = city.Update(request.Name, request.Description, request.RegionId);
        await repository.UpdateAsync(updatedCity, cancellationToken);
        logger.LogInformation("City with id : {CityId} updated.", city.Id);
        return new UpdateCityResponse(city.Id);
    }
}
