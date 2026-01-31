namespace FSH.Framework.Core.Domain;

/// <summary>
/// Base domain event with correlation and tenant context.
/// </summary>
/// <param name="EventId">The unique event identifier.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the event occurred.</param>
/// <param name="CorrelationId">The optional correlation identifier.</param>
/// <param name="TenantId">The optional tenant identifier.</param>
public abstract record DomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string? CorrelationId = null,
    string? TenantId = null
) : IDomainEvent
{
    /// <summary>
    /// Creates a new domain event using the provided factory.
    /// </summary>
    /// <typeparam name="T">The domain event type to create.</typeparam>
    /// <param name="factory">Factory to create the event using the generated id and timestamp.</param>
    /// <returns>The created domain event.</returns>
    public static T Create<T>(Func<Guid, DateTimeOffset, T> factory)
        where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory(Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
