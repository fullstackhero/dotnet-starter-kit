using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Meters.EventHandlers;

public class MeterUpdatedEventHandler(ILogger<MeterUpdatedEventHandler> logger) : INotificationHandler<MeterUpdated>
{
    public async Task Handle(MeterUpdated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling meter updated domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling meter updated domain event..");
    }
}
