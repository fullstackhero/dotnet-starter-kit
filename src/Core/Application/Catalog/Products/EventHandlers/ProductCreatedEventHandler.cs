using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Products.EventHandlers;

public class ProductCreatedEventHandler : INotificationHandler<EventNotification<EntityCreatedEvent<Product>>>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventNotification<EntityCreatedEvent<Product>> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}