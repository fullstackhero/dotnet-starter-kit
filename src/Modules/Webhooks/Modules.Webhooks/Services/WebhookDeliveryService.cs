using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Domain;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace FSH.Modules.Webhooks.Services;

public sealed class WebhookDeliveryService(
    IHttpClientFactory httpClientFactory,
    WebhookDbContext dbContext,
    ILogger<WebhookDeliveryService> logger) : IWebhookDeliveryService
{
    public async Task DeliverAsync(
        Guid subscriptionId,
        string url,
        string? secretHash,
        string eventType,
        string payloadJson,
        CancellationToken ct = default)
    {
        var delivery = WebhookDelivery.Create(subscriptionId, eventType, payloadJson);
        var client = httpClientFactory.CreateClient("Webhooks");

        try
        {
            using var content = new StringContent(payloadJson, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(secretHash))
            {
                var signature = WebhookPayloadSigner.Sign(payloadJson, secretHash);
                content.Headers.Add("X-Webhook-Signature", signature);
            }

            content.Headers.Add("X-Webhook-Event", eventType);
            content.Headers.Add("X-Webhook-Delivery-Id", delivery.Id.ToString());

            var response = await client.PostAsync(new Uri(url), content, ct).ConfigureAwait(false);
            delivery.RecordResult((int)response.StatusCode, response.IsSuccessStatusCode, null);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Webhook delivery {DeliveryId} to {Url} returned {StatusCode}",
                    delivery.Id, url, (int)response.StatusCode);
            }
        }
        // Broad catch is intentional: delivery failures (DNS, timeout, HTTP errors) must be
        // recorded in the delivery log rather than crashing the caller.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            delivery.RecordResult(0, false, ex.Message);
            logger.LogWarning(ex, "Webhook delivery {DeliveryId} to {Url} failed", delivery.Id, url);
        }

        dbContext.Deliveries.Add(delivery);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
