using DN.WebApi.Application.Common.Event;
using DN.WebApi.Domain.Catalog.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Application.Catalog.EventHandlers;

public class ProductCreatedEventHandler : INotificationHandler<EventNotification<ProductCreatedEvent>>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventNotification<ProductCreatedEvent> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}