using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.EventHandlers;

public class NeighborhoodUpdatedEventHandler(ILogger<NeighborhoodUpdatedEventHandler> logger) : INotificationHandler<NeighborhoodUpdated>
{
    public async Task Handle(NeighborhoodUpdated notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling neighborhood updated domain event...");
        await Task.FromResult(notification);
        logger.LogInformation("Finished handling neighborhood updated domain event...");
    }
}
