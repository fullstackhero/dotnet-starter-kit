using FSH.Modules.Catalog.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Catalog.Events;

public sealed class CatalogEventHandlers(ILogger<CatalogEventHandlers> logger) :
    INotificationHandler<ProductCreatedDomainEvent>,
    INotificationHandler<ProductPriceChangedDomainEvent>,
    INotificationHandler<ProductStockAdjustedDomainEvent>
{
    public ValueTask Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProductCreatedDomainEvent for ProductId: {ProductId}", notification.ProductId);
        }
        return default;
    }

    public ValueTask Handle(ProductPriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProductPriceChangedDomainEvent for ProductId: {ProductId}", notification.ProductId);
        }
        return default;
    }

    public ValueTask Handle(ProductStockAdjustedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProductStockAdjustedDomainEvent for ProductId: {ProductId}", notification.ProductId);
        }
        return default;
    }
}
