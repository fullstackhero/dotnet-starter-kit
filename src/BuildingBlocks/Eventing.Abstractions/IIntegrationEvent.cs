namespace FSH.Framework.Eventing.Abstractions;

/// <summary>
/// Base integration event contract used for cross-module and cross-service messaging.
/// </summary>
public interface IIntegrationEvent
{
    Guid Id { get; }

    DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Tenant identifier for tenant-scoped events. Null for global events.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Correlation identifier to tie events to requests and traces.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Logical source of the event (e.g., module or service name).
    /// </summary>
    string Source { get; }
}