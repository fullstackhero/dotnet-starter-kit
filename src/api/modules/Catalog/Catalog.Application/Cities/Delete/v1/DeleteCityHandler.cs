using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Delete.v1;
public sealed class DeleteCityHandler(
    ILogger<DeleteCityHandler> logger,
    [FromKeyedServices("catalog:cities")] IRepository<City> repository)
    : IRequestHandler<DeleteCityCommand>
{
    public async Task Handle(DeleteCityCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var city = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = city ?? throw new CityNotFoundException(request.Id);
        await repository.DeleteAsync(city, cancellationToken);
        logger.LogInformation("City with id : {CityId} deleted", city.Id);
    }
}
