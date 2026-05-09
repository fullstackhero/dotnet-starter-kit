using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.EventHandlers;

public class TariffCreatedEventHandler(ILogger<TariffCreatedEventHandler> logger) : INotificationHandler<TariffCreated>
{
    public async Task Handle(TariffCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling tariff created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling tariff created domain event..");
    }
}
