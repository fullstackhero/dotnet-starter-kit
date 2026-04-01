using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Eventing.Inbox;

/// <summary>
/// EF Core-based inbox store for a specific DbContext.
/// </summary>
/// <typeparam name="TDbContext">The DbContext that owns the InboxMessages set.</typeparam>
public sealed class EfCoreInboxStore<TDbContext>(TDbContext dbContext, TimeProvider timeProvider) : IInboxStore
    where TDbContext : DbContext
{
    public async Task<bool> HasProcessedAsync(Guid eventId, string handlerName, CancellationToken ct = default)
    {
        return await dbContext.Set<InboxMessage>()
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
            ProcessedOnUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        await dbContext.Set<InboxMessage>().AddAsync(message, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}