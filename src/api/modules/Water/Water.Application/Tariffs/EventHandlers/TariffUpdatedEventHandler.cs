using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.EventHandlers;

public class TariffUpdatedEventHandler(ILogger<TariffUpdatedEventHandler> logger) : INotificationHandler<TariffUpdated>
{
    public async Task Handle(TariffUpdated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling tariff updated domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling tariff updated domain event..");
    }
}
