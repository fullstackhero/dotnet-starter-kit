namespace FSH.Modules.Auditing.Contracts;

public interface IAuditEvent
{
    /// <summary>Event category (EntityChange, Security, Activity, Exception…)</summary>
    AuditEventType EventType { get; }

    /// <summary>Severity level (None, Info, Error, …)</summary>
    AuditSeverity Severity { get; }

    /// <summary>UTC time when the event actually occurred.</summary>
    DateTime OccurredAtUtc { get; }

    /// <summary>Tenant identifier (optional in per-tenant DBs; still useful for exports).</summary>
    string? TenantId { get; }

    /// <summary>Subject/User id and display name (when available).</summary>
    string? UserId { get; }
    string? UserName { get; }

    /// <summary>Correlation/trace identifiers for distributed tracing.</summary>
    string? TraceId { get; }
    string? SpanId { get; }
    string? CorrelationId { get; }
    string? RequestId { get; }

    /// <summary>Logical source (module/service) of the event.</summary>
    string? Source { get; }

    /// <summary>Compact bitwise tags (e.g., PiiMasked, Sampled).</summary>
    AuditTag Tags { get; }

    /// <summary>Strongly-typed payload (EntityChange, Security, Activity, Exception, etc.).</summary>
    object Payload { get; }
}