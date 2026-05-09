using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Meters.Delete.v1;

public sealed class DeleteMeterHandler(
    ILogger<DeleteMeterHandler> logger,
    [FromKeyedServices("water:meters")] IRepository<Meter> repository)
    : IRequestHandler<DeleteMeterCommand>
{
    public async Task Handle(DeleteMeterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meter = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = meter ?? throw new MeterNotFoundException(request.Id);
        await repository.DeleteAsync(meter, cancellationToken);
        logger.LogInformation("meter with id : {MeterId} deleted", meter.Id);
    }
}
