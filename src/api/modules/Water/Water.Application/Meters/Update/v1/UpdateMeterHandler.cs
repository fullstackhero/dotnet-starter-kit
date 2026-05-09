using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Meters.Update.v1;

public sealed class UpdateMeterHandler(
    ILogger<UpdateMeterHandler> logger,
    [FromKeyedServices("water:meters")] IRepository<Meter> repository)
    : IRequestHandler<UpdateMeterCommand, UpdateMeterResponse>
{
    public async Task<UpdateMeterResponse> Handle(UpdateMeterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meter = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = meter ?? throw new MeterNotFoundException(request.Id);
        var updatedMeter = meter.Update(request.Model, request.Status, request.LastReadingDate);
        await repository.UpdateAsync(updatedMeter, cancellationToken);
        logger.LogInformation("meter with id : {MeterId} updated.", meter.Id);
        return new UpdateMeterResponse(meter.Id);
    }
}
