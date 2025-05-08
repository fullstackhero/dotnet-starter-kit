using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Create.v1;

public sealed class CreateCityHandler(
    ILogger<CreateCityHandler> logger,
    IRepository<City> repository)
    : IRequestHandler<CreateCityCommand, CreateCityResponse>
{
    public async Task<CreateCityResponse> Handle(CreateCityCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var city = City.Create(request.Name, request.Description, request.RegionId);
        await repository.AddAsync(city, cancellationToken);
        logger.LogInformation("City created {CityId}", city.Id);
        return new CreateCityResponse(city.Id, city.Name, city.Description, city.RegionId);
    }
}
