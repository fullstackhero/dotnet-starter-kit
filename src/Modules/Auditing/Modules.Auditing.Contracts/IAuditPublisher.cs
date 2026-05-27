namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Low-latency, non-blocking publisher. Implement with a bounded channel + background worker.
/// </summary>
public interface IAuditPublisher
{
    /// <summary>Publish an audit event. Implementations should avoid blocking the request path.</summary>
    ValueTask PublishAsync(IAuditEvent auditEvent, CancellationToken ct = default);

    /// <summary>Ambient scope for the current operation (usually request-scoped).</summary>
    IAuditScope CurrentScope { get; }
}