using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Products.EventHandlers;

public class ProductUpdatedEventHandler : INotificationHandler<EventNotification<EntityUpdatedEvent<Product>>>
{
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(ILogger<ProductUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventNotification<EntityUpdatedEvent<Product>> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}