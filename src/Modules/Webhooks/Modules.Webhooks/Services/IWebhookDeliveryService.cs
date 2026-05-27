namespace FSH.Modules.Webhooks.Services;

public interface IWebhookDeliveryService
{
    Task DeliverAsync(Guid subscriptionId, string url, string? secretHash, string eventType, string payloadJson, CancellationToken ct = default);
}
