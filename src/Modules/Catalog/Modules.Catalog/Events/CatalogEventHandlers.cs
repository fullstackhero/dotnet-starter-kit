using System.Text.Json;
using FSH.Framework.Core.Context;
using FSH.Framework.Web.Sse;
using FSH.Modules.Catalog.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Catalog.Events;

public sealed class CatalogEventHandlers(
    ILogger<CatalogEventHandlers> logger,
    SseConnectionManager sse,
    ICurrentUser currentUser) :
    INotificationHandler<ProductCreatedDomainEvent>,
    INotificationHandler<ProductUpdatedDomainEvent>,
    INotificationHandler<ProductDeletedDomainEvent>,
    INotificationHandler<ProductPriceChangedDomainEvent>,
    INotificationHandler<ProductStockAdjustedDomainEvent>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ValueTask Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        logger.LogInformation("Handling ProductCreatedDomainEvent for ProductId: {ProductId}", notification.ProductId);

        BroadcastEvent("ProductCreated", new
        {
            notification.ProductId,
            notification.Sku,
            notification.Name,
        });

        return default;
    }

    public ValueTask Handle(ProductUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        logger.LogInformation("Handling ProductUpdatedDomainEvent for ProductId: {ProductId}", notification.ProductId);

        BroadcastEvent("ProductUpdated", new
        {
            notification.ProductId,
            notification.Name,
        });

        return default;
    }

    public ValueTask Handle(ProductDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        logger.LogInformation("Handling ProductDeletedDomainEvent for ProductId: {ProductId}", notification.ProductId);

        BroadcastEvent("ProductDeleted", new
        {
            notification.ProductId,
        });

        return default;
    }

    public ValueTask Handle(ProductPriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        logger.LogInformation("Handling ProductPriceChangedDomainEvent for ProductId: {ProductId}", notification.ProductId);

        BroadcastEvent("ProductPriceChanged", new
        {
            notification.ProductId,
            notification.OldAmount,
            notification.NewAmount,
            notification.Currency,
        });

        return default;
    }

    public ValueTask Handle(ProductStockAdjustedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        logger.LogInformation("Handling ProductStockAdjustedDomainEvent for ProductId: {ProductId}", notification.ProductId);

        BroadcastEvent("ProductStockAdjusted", new
        {
            notification.ProductId,
            notification.OldStock,
            notification.NewStock,
            notification.Delta,
        });

        return default;
    }

    /// <summary>
    /// Broadcasts an SSE event scoped to the current tenant. Falls back to a global broadcast
    /// when the tenant context is unavailable (e.g. background jobs without an HTTP context).
    /// </summary>
    private void BroadcastEvent(string eventType, object payload)
    {
        string data = JsonSerializer.Serialize(payload, JsonOptions);
        var sseEvent = new SseEvent(eventType, data);

        string? tenantId = currentUser.GetTenant();
        if (!string.IsNullOrEmpty(tenantId))
        {
            sse.Broadcast(tenantId, sseEvent);
        }
        else
        {
            sse.BroadcastAll(sseEvent);
        }
    }
}
