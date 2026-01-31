namespace FSH.Framework.Core.Domain;

/// <summary>
/// Represents a domain event with correlation and tenant context.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOnUtc { get; }

    /// <summary>
    /// Gets the correlation identifier for tracing across boundaries.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the tenant identifier associated with the event.
    /// </summary>
    string? TenantId { get; }
}
