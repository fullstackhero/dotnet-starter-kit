using Hangfire;

namespace FSH.Modules.Webhooks.Services;

public sealed class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IBackgroundJobClient _jobs;

    public WebhookDispatcher(IBackgroundJobClient jobs) => _jobs = jobs;

    public Task EnqueueAsync(string tenantId, Guid subscriptionId, string eventType, string payloadJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        // Hangfire injects PerformContext and CancellationToken at execution time.
        _jobs.Enqueue<WebhookDispatchJob>(j =>
            j.DispatchAsync(subscriptionId, tenantId, eventType, payloadJson, null!, CancellationToken.None));
        return Task.CompletedTask;
    }
}
