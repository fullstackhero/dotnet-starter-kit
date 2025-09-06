using FSH.Framework.Core.Messaging.Events;
using FSH.Modules.Common.Core.Domain.Contracts;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSH.Framework.Core.Domain;

public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; protected init; } = default!;
    [NotMapped]
    public Collection<AppEvent> DomainEvents { get; } = new Collection<AppEvent>();
    public void QueueDomainEvent(AppEvent @event)
    {
        if (!DomainEvents.Contains(@event))
            DomainEvents.Add(@event);
    }
}

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity() => Id = Guid.NewGuid();
}