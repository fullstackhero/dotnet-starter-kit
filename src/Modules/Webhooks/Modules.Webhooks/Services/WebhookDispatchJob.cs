using System.Net.Http.Headers;
using System.Text;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Domain;
using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Webhooks.Services;

/// <summary>
/// Hangfire job that delivers a webhook payload. Throws on transient failure so Hangfire
/// reschedules the attempt; non-transient failures (4xx, missing subscription) complete
/// silently to stop the retry loop. Each attempt persists its own <see cref="WebhookDelivery"/>
/// row so the delivery log retains per-attempt detail.
/// </summary>
public sealed class WebhookDispatchJob
{
    private const string HttpClientName = "Webhooks";
    private const int MaxRetries = 4;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebhookSecretProtector _secretProtector;
    private readonly ILogger<WebhookDispatchJob> _logger;

    public WebhookDispatchJob(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IWebhookSecretProtector secretProtector,
        ILogger<WebhookDispatchJob> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _secretProtector = secretProtector;
        _logger = logger;
    }

    // 4 retries after the initial attempt → up to 5 total. Exponential backoff:
    // 30s, 2m, 10m, 1h. After exhaustion the job fails and lands in Hangfire's failed queue.
    [AutomaticRetry(
        Attempts = MaxRetries,
        DelaysInSeconds = new[] { 30, 120, 600, 3600 },
        OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task DispatchAsync(
        Guid subscriptionId,
        string tenantId,
        string eventType,
        string payloadJson,
        PerformContext? context,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        // Like SqlAuditSink: fresh scope, set tenant context first, then resolve the DbContext so its
        // Finbuckle filter reads a real TenantInfo instead of a null one from the outer scope.
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await store.GetAsync(tenantId).ConfigureAwait(false);
        if (tenant is null)
        {
            _logger.LogWarning(
                "Skipping webhook dispatch for subscription {SubscriptionId}: tenant '{TenantId}' not found.",
                subscriptionId, tenantId);
            return;
        }
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();

        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken)
            .ConfigureAwait(false);

        if (subscription is null || !subscription.IsActive)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Skipping webhook dispatch for subscription {SubscriptionId} (not found or inactive).",
                    subscriptionId);
            }
            return;
        }

        // Hangfire stores RetryCount on the job once a retry has been scheduled.
        // First execution → no RetryCount → attempt 1.
        var retryCount = context?.GetJobParameter<int?>("RetryCount") ?? 0;
        var attemptNumber = retryCount + 1;

        var delivery = WebhookDelivery.Create(subscriptionId, eventType, payloadJson, attemptNumber);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var content = new StringContent(payloadJson, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
            var signingSecret = _secretProtector.Unprotect(subscription.ProtectedSecret);
            if (!string.IsNullOrEmpty(signingSecret))
            {
                content.Headers.Add("X-Webhook-Signature", WebhookPayloadSigner.Sign(payloadJson, signingSecret));
            }
            content.Headers.Add("X-Webhook-Event", eventType);
            content.Headers.Add("X-Webhook-Delivery-Id", delivery.Id.ToString());

            var response = await client.PostAsync(new Uri(subscription.Url), content, cancellationToken).ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;
            delivery.RecordResult(statusCode, response.IsSuccessStatusCode, null);

            dbContext.Deliveries.Add(delivery);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Webhook delivery {DeliveryId} attempt {Attempt} to {Url} succeeded ({StatusCode}).",
                        delivery.Id, attemptNumber, subscription.Url, statusCode);
                }
                return;
            }

            if (IsTransient(statusCode))
            {
                throw new WebhookDeliveryFailedException(
                    $"Webhook delivery to {subscription.Url} returned transient status {statusCode}; will retry (attempt {attemptNumber}/{MaxRetries + 1}).");
            }

            // Permanent client error (4xx other than 408/429) — don't retry.
            _logger.LogWarning(
                "Webhook delivery {DeliveryId} to {Url} got non-retryable status {StatusCode}; giving up.",
                delivery.Id, subscription.Url, statusCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (WebhookDeliveryFailedException)
        {
            throw;
        }
        // Network errors, DNS failures, timeouts — transient. Record the attempt then rethrow
        // so Hangfire reschedules the next retry.
        catch (Exception ex)
        {
            delivery.RecordResult(0, false, ex.Message);
            dbContext.Deliveries.Add(delivery);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogWarning(ex,
                "Webhook delivery {DeliveryId} attempt {Attempt} to {Url} failed transiently.",
                delivery.Id, attemptNumber, subscription.Url);

            throw new WebhookDeliveryFailedException("Transient webhook delivery failure.", ex);
        }
    }

    private static bool IsTransient(int statusCode) =>
        statusCode >= 500 || statusCode == 408 || statusCode == 429;
}

public sealed class WebhookDeliveryFailedException : Exception
{
    public WebhookDeliveryFailedException() { }
    public WebhookDeliveryFailedException(string message) : base(message) { }
    public WebhookDeliveryFailedException(string message, Exception innerException) : base(message, innerException) { }
}
