using Finbuckle.MultiTenant;
using FSH.Framework.Abstractions.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Infrastructure.Persistence;
public class FshDbContext : MultiTenantDbContext
{
    private readonly IPublisher _publisher;
    public FshDbContext(ITenantInfo currentTenant, DbContextOptions options, IPublisher publisher)
        : base(currentTenant, options)
    {
        _publisher = publisher;
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublishDomainEventsAsync().ConfigureAwait(false);
        return result;
    }
    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker.Entries<IEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .SelectMany(e =>
            {
                var domainEvents = e.DomainEvents.ToList();
                e.DomainEvents.Clear();
                return domainEvents;
            })
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent).ConfigureAwait(false);
        }
    }
}
