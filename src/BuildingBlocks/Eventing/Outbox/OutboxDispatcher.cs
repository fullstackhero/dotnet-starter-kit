using FSH.Framework.Eventing.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// Dispatches outbox messages via the configured event bus.
/// This type is intended to be invoked by a scheduler (e.g., Hangfire recurring job or hosted service).
/// </summary>
public sealed partial class OutboxDispatcher
{
    private readonly IOutboxStore _outbox;
    private readonly IEventBus _bus;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly EventingOptions _options;

    public OutboxDispatcher(
        IOutboxStore outbox,
        IEventBus bus,
        IEventSerializer serializer,
        IOptions<EventingOptions> options,
        ILogger<OutboxDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _outbox = outbox;
        _bus = bus;
        _serializer = serializer;
        _logger = logger;
        _options = options.Value;
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

        LogDispatching(messages.Count, batchSize);

        var processedCount = 0;
        var failedCount = 0;
        var deadLetterCount = 0;

        foreach (var message in messages)
        {
            try
            {
                var @event = _serializer.Deserialize(message.Payload, message.Type);
                if (@event is null)
                {
                    await _outbox.MarkAsFailedAsync(message, "Cannot deserialize integration event.", isDead: true, ct).ConfigureAwait(false);
                    continue;
                }

                await _bus.PublishAsync(@event, ct).ConfigureAwait(false);
                await _outbox.MarkAsProcessedAsync(message, ct).ConfigureAwait(false);
                processedCount++;

                LogMessageDispatched(message.Id);
            }
            // Broad catch is intentional: each message must be processed independently,
            // and any failure type should trigger the retry/dead-letter mechanism.
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

        LogDispatchSummary(messages.Count, processedCount, failedCount, deadLetterCount);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Dispatching {Count} outbox messages (BatchSize={BatchSize})")]
    private partial void LogDispatching(int count, int batchSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox message {MessageId} dispatched and marked as processed.")]
    private partial void LogMessageDispatched(Guid messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox dispatch summary: Total={Total}, Processed={Processed}, Failed={Failed}, DeadLettered={DeadLettered}")]
    private partial void LogDispatchSummary(int total, int processed, int failed, int deadLettered);
}