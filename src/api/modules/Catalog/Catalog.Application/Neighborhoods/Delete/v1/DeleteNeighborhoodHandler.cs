using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Delete.v1;

public sealed class DeleteNeighborhoodHandler(
    ILogger<DeleteNeighborhoodHandler> logger,
    IRepository<Neighborhood> repository)
    : IRequestHandler<DeleteNeighborhoodCommand>
{
    public async Task Handle(DeleteNeighborhoodCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var neighborhood = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NeighborhoodNotFoundException(request.Id);
        await repository.DeleteAsync(neighborhood, cancellationToken);
        logger.LogInformation("Neighborhood deleted {NeighborhoodId}", neighborhood.Id);
    }
}