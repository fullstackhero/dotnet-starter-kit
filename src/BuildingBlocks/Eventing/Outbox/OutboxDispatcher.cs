using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// Dispatches outbox messages via the configured event bus.
/// This type is intended to be invoked by a scheduler (e.g., Hangfire recurring job or hosted service).
/// </summary>
public sealed class OutboxDispatcher
{
    private readonly IOutboxStore _outbox;
    private readonly IEventBus _bus;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly EventingOptions _options;
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly IMultiTenantContextSetter _tenantContextSetter;

    public OutboxDispatcher(
        IOutboxStore outbox,
        IEventBus bus,
        IEventSerializer serializer,
        IOptions<EventingOptions> options,
        ILogger<OutboxDispatcher> logger,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IMultiTenantContextSetter tenantContextSetter)
    {
        ArgumentNullException.ThrowIfNull(options);

        _outbox = outbox;
        _bus = bus;
        _serializer = serializer;
        _logger = logger;
        _options = options.Value;
        _tenantStore = tenantStore;
        _tenantContextSetter = tenantContextSetter;
    }

    public async Task DispatchAsync(CancellationToken ct = default)
    {
        var batchSize = _options.OutboxBatchSize;
        if (batchSize <= 0) batchSize = 100;

        var messages = await _outbox.GetPendingBatchAsync(batchSize, ct).ConfigureAwait(false);
        if (messages.Count == 0)
        {
            _logger.LogDebug("No outbox messages to dispatch.");
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Dispatching {Count} outbox messages (BatchSize={BatchSize})", messages.Count, batchSize);
        }

        var processedCount = 0;
        var failedCount = 0;
        var deadLetterCount = 0;

        foreach (var message in messages)
        {
            try
            {
                if (!string.IsNullOrEmpty(message.TenantId))
                {
                    var tenantInfo = await _tenantStore.GetAsync(message.TenantId).ConfigureAwait(false);
                    if (tenantInfo is not null)
                        _tenantContextSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenantInfo);
                }

                var @event = _serializer.Deserialize(message.Payload, message.Type);
                if (@event is null)
                {
                    await _outbox.MarkAsFailedAsync(message, "Cannot deserialize integration event.", isDead: true, ct).ConfigureAwait(false);
                    continue;
                }

                await _bus.PublishAsync(@event, ct).ConfigureAwait(false);
                await _outbox.MarkAsProcessedAsync(message, ct).ConfigureAwait(false);
                processedCount++;

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Outbox message {MessageId} dispatched and marked as processed.", message.Id);
                }
            }
            catch (Exception ex)
            {
                var maxRetries = _options.OutboxMaxRetries <= 0 ? 5 : _options.OutboxMaxRetries;
                var isDead = message.RetryCount + 1 >= maxRetries;

                await _outbox.MarkAsFailedAsync(message, ex.Message, isDead, ct).ConfigureAwait(false);

                failedCount++;
                if (isDead)
                {
                    deadLetterCount++;
                }

                if (isDead)
                {
                    _logger.LogError(ex, "Outbox message {MessageId} moved to dead-letter after {RetryCount} retries", message.Id, message.RetryCount + 1);
                }
                else
                {
                    _logger.LogWarning(ex, "Outbox message {MessageId} failed (RetryCount={RetryCount}).", message.Id, message.RetryCount + 1);
                }
            }
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Outbox dispatch summary: Total={Total}, Processed={Processed}, Failed={Failed}, DeadLettered={DeadLettered}",
                messages.Count,
                processedCount,
                failedCount,
                deadLetterCount);
        }
    }
}
