using DN.WebApi.Application.Common.Events;
using DN.WebApi.Domain.Catalog.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Application.Catalog.Products;

public class ProductDeletedEventHandler : INotificationHandler<EventNotification<ProductDeletedEvent>>
{
    private readonly ILogger<ProductDeletedEventHandler> _logger;

    public ProductDeletedEventHandler(ILogger<ProductDeletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventNotification<ProductDeletedEvent> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}