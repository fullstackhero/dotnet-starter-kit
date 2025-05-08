using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.EventHandlers;

public class ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger) : INotificationHandler<RegionCreated>
{
    public async Task Handle(RegionCreated notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("handling region created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling region created domain event..");
    }
}

