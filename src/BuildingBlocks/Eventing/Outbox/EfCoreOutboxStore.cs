using FSH.Framework.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// EF Core-based outbox store for a specific DbContext.
/// </summary>
/// <typeparam name="TDbContext">The DbContext that owns the OutboxMessages set.</typeparam>
public sealed class EfCoreOutboxStore<TDbContext>(
    TDbContext dbContext,
    IEventSerializer serializer,
    ILogger<EfCoreOutboxStore<TDbContext>> logger,
    TimeProvider timeProvider) : IOutboxStore
    where TDbContext : DbContext
{
    public async Task AddAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var payload = serializer.Serialize(@event);
        var message = new OutboxMessage
        {
            Id = @event.Id,
            CreatedOnUtc = @event.OccurredOnUtc,
            Type = @event.GetType().AssemblyQualifiedName ?? @event.GetType().FullName!,
            Payload = payload,
            TenantId = @event.TenantId,
            CorrelationId = @event.CorrelationId,
            RetryCount = 0,
            IsDead = false
        };

        await dbContext.Set<OutboxMessage>().AddAsync(message, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingBatchAsync(int batchSize, CancellationToken ct = default)
    {
        return await dbContext.Set<OutboxMessage>()
            .Where(m => !m.IsDead && m.ProcessedOnUtc == null)
            .OrderBy(m => m.CreatedOnUtc)
            .Take(batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task MarkAsProcessedAsync(OutboxMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ProcessedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        dbContext.Set<OutboxMessage>().Update(message);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task MarkAsFailedAsync(OutboxMessage message, string error, bool isDead, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.RetryCount++;
        message.LastError = error;
        message.IsDead = isDead;
        dbContext.Set<OutboxMessage>().Update(message);

        logger.LogWarning("Outbox message {MessageId} failed. RetryCount={RetryCount}, IsDead={IsDead}, Error={Error}",
            message.Id, message.RetryCount, message.IsDead, error);

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}