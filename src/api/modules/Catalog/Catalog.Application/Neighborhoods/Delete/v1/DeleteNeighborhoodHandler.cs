using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Delete.v1;
public sealed class DeleteNeighborhoodHandler(
    ILogger<DeleteNeighborhoodHandler> logger,
    [FromKeyedServices("catalog:neighborhoods")] IRepository<Neighborhood> repository)
    : IRequestHandler<DeleteNeighborhoodCommand>
{
    public async Task Handle(DeleteNeighborhoodCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var neighborhood = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = neighborhood ?? throw new NeighborhoodNotFoundException(request.Id);
        await repository.DeleteAsync(neighborhood, cancellationToken);
        logger.LogInformation("Neighborhood with id : {NeighborhoodId} deleted", neighborhood.Id);
    }
}
