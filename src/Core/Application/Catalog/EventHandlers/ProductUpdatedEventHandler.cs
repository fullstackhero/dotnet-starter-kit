using DN.WebApi.Application.Common.Event;
using DN.WebApi.Domain.Catalog.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Application.Catalog.EventHandlers;

public class ProductUpdatedEventHandler : INotificationHandler<EventNotification<ProductUpdatedEvent>>
{
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(ILogger<ProductUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventNotification<ProductUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}