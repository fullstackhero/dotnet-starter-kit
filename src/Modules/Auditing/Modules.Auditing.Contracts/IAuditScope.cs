namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Ambient context for the current operation/request. 
/// Implementations typically pull from HttpContext, Tenant provider, and Activity.Current.
/// </summary>
public interface IAuditScope
{
    string? TenantId { get; }
    string? UserId { get; }
    string? UserName { get; }
    string? TraceId { get; }
    string? SpanId { get; }
    string? CorrelationId { get; }
    string? RequestId { get; }
    string? Source { get; }

    /// <summary>Default tags to apply to all events in this scope.</summary>
    AuditTag Tags { get; }

    /// <summary>Clone the scope with additional tags (non-destructive).</summary>
    IAuditScope WithTags(AuditTag tags);

    /// <summary>Clone the scope overriding select fields (use null to keep existing).</summary>
    IAuditScope WithProperties(
        string? tenantId = null,
        string? userId = null,
        string? userName = null,
        string? traceId = null,
        string? spanId = null,
        string? correlationId = null,
        string? requestId = null,
        string? source = null,
        AuditTag? tags = null);
}