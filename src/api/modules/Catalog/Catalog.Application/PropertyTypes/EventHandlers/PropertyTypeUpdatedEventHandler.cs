using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.EventHandlers;

public class PropertyTypeUpdatedEventHandler(ILogger<PropertyTypeUpdatedEventHandler> logger) : INotificationHandler<PropertyTypeUpdated>
{
    public async Task Handle(PropertyTypeUpdated notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling property type updated domain event...");
        await Task.FromResult(notification);
        logger.LogInformation("Finished handling property type updated domain event...");
    }
}
