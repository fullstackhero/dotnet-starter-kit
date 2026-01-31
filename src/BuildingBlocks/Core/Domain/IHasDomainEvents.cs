namespace FSH.Framework.Core.Domain;

/// <summary>
/// Exposes domain events raised by an entity.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the collection of raised domain events.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears the stored domain events.
    /// </summary>
    void ClearDomainEvents();
}
