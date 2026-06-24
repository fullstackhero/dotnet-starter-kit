namespace FSH.Modules.Webhooks.Services;

public interface IWebhookDeliveryService
{
    Task DeliverAsync(Guid subscriptionId, string url, string? signingSecret, string eventType, string payloadJson, CancellationToken ct = default);
}
