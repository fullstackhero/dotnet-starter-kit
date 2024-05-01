using Finbuckle.MultiTenant.EntityFrameworkCore;
using FSH.Framework.Abstractions.Domain;
using FSH.Framework.Core.Configurations;
using FSH.Framework.Core.Tenant.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Persistence;
public class FshDbContext(IFshTenantInfo currentTenant,
    DbContextOptions options,
    IPublisher publisher,
    IOptions<DatabaseOptions> settings)
    : MultiTenantDbContext(currentTenant, options)
{
    private readonly IPublisher _publisher = publisher;
    private readonly DatabaseOptions _settings = settings.Value;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();

        if (!string.IsNullOrWhiteSpace(currentTenant?.ConnectionString))
        {
            optionsBuilder.ConfigureDatabase(_settings.Provider, currentTenant.ConnectionString);
        }
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
