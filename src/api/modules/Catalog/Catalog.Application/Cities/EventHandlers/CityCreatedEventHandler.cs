using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.EventHandlers;

public class CityCreatedEventHandler(ILogger<CityCreatedEventHandler> logger) : INotificationHandler<CityCreated>
{
    public async Task Handle(CityCreated notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("handling city created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling city created domain event..");
    }
}

