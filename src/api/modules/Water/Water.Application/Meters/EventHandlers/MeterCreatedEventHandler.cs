using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Meters.EventHandlers;

public class MeterCreatedEventHandler(ILogger<MeterCreatedEventHandler> logger) : INotificationHandler<MeterCreated>
{
    public async Task Handle(MeterCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling meter created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling meter created domain event..");
    }
}
