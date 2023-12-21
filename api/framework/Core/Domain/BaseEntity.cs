using System.ComponentModel.DataAnnotations.Schema;
using FSH.Framework.Abstractions.Domain;
using FSH.Framework.Core.Domain.Events;

namespace FSH.Framework.Core.Domain;

public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; protected init; } = default!;
    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new List<DomainEvent>();
    public void QueueDomainEvent(DomainEvent @event)
    {
        if (!DomainEvents.Contains(@event))
            DomainEvents.Add(@event);
    }
}

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity() => Id = Guid.NewGuid();
}
