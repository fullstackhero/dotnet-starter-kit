using FSH.Framework.Abstractions.Domain;
using FSH.WebApi.Todo.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Todo.Data;
public class TodoDbContext : DbContext
{
    private readonly IPublisher _publisher;
    public TodoDbContext(DbContextOptions<TodoDbContext> options, IPublisher publisher) : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<TodoItem> Todos { get; set; } = null!;

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
