using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Update.v1;
public sealed class UpdateAgencyHandler(
    ILogger<UpdateAgencyHandler> logger,
    [FromKeyedServices("catalog:agencies")] IRepository<Agency> repository)
    : IRequestHandler<UpdateAgencyCommand, UpdateAgencyResponse>
{
    public async Task<UpdateAgencyResponse> Handle(UpdateAgencyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agency = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = agency ?? throw new AgencyNotFoundException(request.Id);
        var updatedAgency = agency.Update(request.Name, request.Email, request.Telephone, request.Address);
        await repository.UpdateAsync(updatedAgency, cancellationToken);
        logger.LogInformation("Agency with id : {AgencyId} updated.", agency.Id);
        return new UpdateAgencyResponse(agency.Id);
    }
}
