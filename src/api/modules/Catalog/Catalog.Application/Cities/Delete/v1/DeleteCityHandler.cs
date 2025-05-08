using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Delete.v1;

public sealed class DeleteCityHandler(
    ILogger<DeleteCityHandler> logger,
    IRepository<City> repository)
    : IRequestHandler<DeleteCityCommand>
{
    public async Task Handle(DeleteCityCommand request, CancellationToken cancellationToken)
    {
          ArgumentNullException.ThrowIfNull(request);
        var city = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = city ?? throw new ProductNotFoundException(request.Id);
        await repository.DeleteAsync(city, cancellationToken);
        logger.LogInformation("product with id : {ProductId} deleted", city.Id);
    }
}
