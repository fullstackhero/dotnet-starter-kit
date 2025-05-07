using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Delete.v1;

public sealed class DeleteAgencyHandler(
    ILogger<DeleteAgencyHandler> logger,
    [FromKeyedServices("catalog:agencies")] IRepository<Agency> repository)
    : IRequestHandler<DeleteAgencyCommand>
{
    public async Task<Unit> Handle(DeleteAgencyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agency = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (agency == null)
        {
            throw new NotFoundException($"Agency with ID {request.Id} not found.");
        }

        await repository.DeleteAsync(agency, cancellationToken);
        logger.LogInformation("Agency deleted {AgencyId}", agency.Id);
        return Unit.Value;
    }

    Task IRequestHandler<DeleteAgencyCommand>.Handle(DeleteAgencyCommand request, CancellationToken cancellationToken)
    {
        return Handle(request, cancellationToken);
    }
}
