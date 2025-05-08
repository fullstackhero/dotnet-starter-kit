using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.EventHandlers;

public class NeighborhoodCreatedEventHandler(ILogger<NeighborhoodCreatedEventHandler> logger) : INotificationHandler<NeighborhoodCreated>
{
    public async Task Handle(NeighborhoodCreated notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling neighborhood created domain event...");
        await Task.FromResult(notification);
        logger.LogInformation("Finished handling neighborhood created domain event...");
    }
}
