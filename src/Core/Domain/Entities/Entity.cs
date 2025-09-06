using FSH.Framework.Core.Domain.Interfaces;

namespace FSH.Framework.Core.Domain.Entities;

public abstract class Entity<TId> : IHasDomainEvents
{
    public TId Id { get; protected set; } = default!;
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void QueueDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}