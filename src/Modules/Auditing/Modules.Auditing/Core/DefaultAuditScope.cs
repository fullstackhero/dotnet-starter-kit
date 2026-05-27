using FSH.Modules.Auditing.Contracts;

namespace FSH.Modules.Auditing;

/// <summary>
/// Immutable, minimal scope implementation. Create per request/operation.
/// </summary>
public sealed record DefaultAuditScope(
    string? TenantId,
    string? UserId,
    string? UserName,
    string? TraceId,
    string? SpanId,
    string? CorrelationId,
    string? RequestId,
    string? Source,
    AuditTag Tags
) : IAuditScope
{
    public IAuditScope WithTags(AuditTag tags) => this with { Tags = this.Tags | tags };

    public IAuditScope WithProperties(
        string? tenantId = null,
        string? userId = null,
        string? userName = null,
        string? traceId = null,
        string? spanId = null,
        string? correlationId = null,
        string? requestId = null,
        string? source = null,
        AuditTag? tags = null)
        => this with
        {
            TenantId = tenantId ?? this.TenantId,
            UserId = userId ?? this.UserId,
            UserName = userName ?? this.UserName,
            TraceId = traceId ?? this.TraceId,
            SpanId = spanId ?? this.SpanId,
            CorrelationId = correlationId ?? this.CorrelationId,
            RequestId = requestId ?? this.RequestId,
            Source = source ?? this.Source,
            Tags = tags ?? this.Tags
        };
}