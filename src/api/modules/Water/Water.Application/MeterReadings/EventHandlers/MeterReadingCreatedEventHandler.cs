using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.EventHandlers;

public class MeterReadingCreatedEventHandler(ILogger<MeterReadingCreatedEventHandler> logger) : INotificationHandler<MeterReadingCreated>
{
    public async Task Handle(MeterReadingCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling meter reading created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling meter reading created domain event..");
    }
}
