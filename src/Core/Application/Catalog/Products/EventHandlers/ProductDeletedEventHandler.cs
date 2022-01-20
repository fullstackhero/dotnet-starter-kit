using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Products.EventHandlers;

public class ProductDeletedEventHandler : INotificationHandler<EventNotification<EntityDeletedEvent<Product>>>
{
    private readonly ILogger<ProductDeletedEventHandler> _logger;

    public ProductDeletedEventHandler(ILogger<ProductDeletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventNotification<EntityDeletedEvent<Product>> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}