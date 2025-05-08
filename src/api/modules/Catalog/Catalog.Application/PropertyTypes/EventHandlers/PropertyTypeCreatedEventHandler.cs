using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.EventHandlers;

public class PropertyTypeCreatedEventHandler(ILogger<PropertyTypeCreatedEventHandler> logger) : INotificationHandler<PropertyTypeCreated>
{
    public async Task Handle(PropertyTypeCreated notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling property type created domain event...");
        await Task.FromResult(notification);
        logger.LogInformation("Finished handling property type created domain event...");
    }
}
