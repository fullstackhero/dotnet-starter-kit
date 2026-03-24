using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Eventing.Inbox;

/// <summary>
/// EF Core-based inbox store for a specific DbContext.
/// </summary>
/// <typeparam name="TDbContext">The DbContext that owns the InboxMessages set.</typeparam>
public sealed class EfCoreInboxStore<TDbContext> : IInboxStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreInboxStore(TDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HasProcessedAsync(Guid eventId, string handlerName, CancellationToken ct = default)
    {
        return await _dbContext.Set<InboxMessage>()
            .AnyAsync(i => i.Id == eventId && i.HandlerName == handlerName, ct)
            .ConfigureAwait(false);
    }

    public async Task MarkProcessedAsync(Guid eventId, string handlerName, string? tenantId, string eventType, CancellationToken ct = default)
    {
        var message = new InboxMessage
        {
            Id = eventId,
            EventType = eventType,
            HandlerName = handlerName,
            TenantId = tenantId,
            ProcessedOnUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _dbContext.Set<InboxMessage>().AddAsync(message, ct).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

