using FSH.Framework.Eventing.Abstractions;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// Abstraction for persisting and reading outbox messages.
/// </summary>
public interface IOutboxStore
{
    Task AddAsync(IIntegrationEvent @event, CancellationToken ct = default);

    Task<IReadOnlyList<OutboxMessage>> GetPendingBatchAsync(int batchSize, CancellationToken ct = default);

    Task MarkAsProcessedAsync(OutboxMessage message, CancellationToken ct = default);

    Task MarkAsFailedAsync(OutboxMessage message, string error, bool isDead, CancellationToken ct = default);
}