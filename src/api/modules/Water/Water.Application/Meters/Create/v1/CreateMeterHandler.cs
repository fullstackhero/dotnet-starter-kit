using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Meters.Create.v1;

public sealed class CreateMeterHandler(
    ILogger<CreateMeterHandler> logger,
    [FromKeyedServices("water:meters")] IRepository<Meter> repository)
    : IRequestHandler<CreateMeterCommand, CreateMeterResponse>
{
    public async Task<CreateMeterResponse> Handle(CreateMeterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meter = Meter.Create(request.MeterNumber, request.Model, request.InstallationDate, request.CustomerId);
        await repository.AddAsync(meter, cancellationToken);
        logger.LogInformation("meter created {MeterId}", meter.Id);
        return new CreateMeterResponse(meter.Id);
    }
}
