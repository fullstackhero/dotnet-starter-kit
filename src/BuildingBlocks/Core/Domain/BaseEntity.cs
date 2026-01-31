namespace FSH.Framework.Core.Domain;

/// <summary>
/// Provides a base implementation for entities with identity and domain events.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class BaseEntity<TId> : IEntity<TId>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Gets the domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>
    /// Raises and records a domain event for later dispatch.
    /// </summary>
    /// <param name="event">The domain event to add.</param>
    protected void AddDomainEvent(IDomainEvent @event)
        => _domainEvents.Add(@event);

    /// <summary>
    /// Clears all recorded domain events.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
