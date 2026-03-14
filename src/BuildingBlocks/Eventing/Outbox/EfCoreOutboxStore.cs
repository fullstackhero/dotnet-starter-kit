using FSH.Framework.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// EF Core-based outbox store for a specific DbContext.
/// </summary>
/// <typeparam name="TDbContext">The DbContext that owns the OutboxMessages set.</typeparam>
public sealed class EfCoreOutboxStore<TDbContext> : IOutboxStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<EfCoreOutboxStore<TDbContext>> _logger;

    public EfCoreOutboxStore(
        TDbContext dbContext,
        IEventSerializer serializer,
        ILogger<EfCoreOutboxStore<TDbContext>> logger)
    {
        _dbContext = dbContext;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task AddAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var payload = _serializer.Serialize(@event);
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

        await _dbContext.Set<OutboxMessage>().AddAsync(message, ct).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingBatchAsync(int batchSize, CancellationToken ct = default)
    {
        try
        {
            return await _dbContext.Set<OutboxMessage>()
                .Where(m => !m.IsDead && m.ProcessedOnUtc == null)
                .OrderBy(m => m.CreatedOnUtc)
                .Take(batchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
        catch (System.Data.Common.DbException ex) when (ex.SqlState == "42P01")
        {
            // Note: This error ("relation does not exist") is expected during startup/migrations,
            // especially when spinning up test containers, as the background outbox dispatcher 
            // might fire before the database schema is fully created.
            // We gracefully return an empty list until the tables are ready.
            _logger.LogDebug(ex, "Outbox table does not exist yet. Skipping dispatch.");
            return [];
        }
    }

    public async Task MarkAsProcessedAsync(OutboxMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ProcessedOnUtc = DateTime.UtcNow;
        _dbContext.Set<OutboxMessage>().Update(message);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task MarkAsFailedAsync(OutboxMessage message, string error, bool isDead, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.RetryCount++;
        message.LastError = error;
        message.IsDead = isDead;
        _dbContext.Set<OutboxMessage>().Update(message);

        _logger.LogWarning("Outbox message {MessageId} failed. RetryCount={RetryCount}, IsDead={IsDead}, Error={Error}",
            message.Id, message.RetryCount, message.IsDead, error);

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

