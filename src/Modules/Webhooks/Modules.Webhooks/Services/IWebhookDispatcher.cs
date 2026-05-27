namespace FSH.Modules.Webhooks.Services;

/// <summary>
/// Production-side webhook dispatch entry point. Enqueues a background job that delivers the
/// payload with automatic retries on transient failure (5xx, 408, 429, network errors).
/// Use this for event-driven dispatch; for synchronous user-triggered tests, call
/// <see cref="IWebhookDeliveryService"/> directly.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>
    /// Enqueues a webhook delivery for the given tenant + subscription. Returns immediately;
    /// actual delivery (and any retries) happens in the background. Tenant is explicit so the
    /// background job can restore the tenant context before touching <see cref="Data.WebhookDbContext"/>
    /// — its Finbuckle query filter and tenant-id auto-write both require it.
    /// </summary>
    Task EnqueueAsync(string tenantId, Guid subscriptionId, string eventType, string payloadJson, CancellationToken cancellationToken = default);
}
