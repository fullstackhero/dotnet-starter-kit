using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Create.v1;

public sealed class CreateAgencyHandler(
    ILogger<CreateAgencyHandler> logger,
    [FromKeyedServices("catalog:agencies")] IRepository<Agency> repository)
    : IRequestHandler<CreateAgencyCommand, CreateAgencyResponse>
{
    public async Task<CreateAgencyResponse> Handle(CreateAgencyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agency = Agency.Create(request.Name!, request.Email!, request.Telephone!, request.Address!);
        await repository.AddAsync(agency, cancellationToken);
        logger.LogInformation("Agency created {AgencyId}", agency.Id);
        return new CreateAgencyResponse(agency.Id, agency.Name);
    }
}
