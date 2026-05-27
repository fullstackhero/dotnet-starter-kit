using System.Diagnostics;

namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Concrete event instance ready to be published/persisted.
/// Carries normalized metadata + strongly-typed Payload.
/// </summary>
public sealed class AuditEnvelope : IAuditEvent
{
    public Guid Id { get; }
    public DateTime OccurredAtUtc { get; }
    public DateTime ReceivedAtUtc { get; }

    public AuditEventType EventType { get; }
    public AuditSeverity Severity { get; }

    public string? TenantId { get; }
    public string? UserId { get; }
    public string? UserName { get; }

    public string? TraceId { get; }
    public string? SpanId { get; }
    public string? CorrelationId { get; }
    public string? RequestId { get; }
    public string? Source { get; }

    public AuditTag Tags { get; }

    public object Payload { get; }

    public AuditEnvelope(
        Guid id,
        DateTime occurredAtUtc,
        DateTime receivedAtUtc,
        AuditEventType eventType,
        AuditSeverity severity,
        string? tenantId,
        string? userId,
        string? userName,
        string? traceId,
        string? spanId,
        string? correlationId,
        string? requestId,
        string? source,
        AuditTag tags,
        object payload)
    {
        Id = id;
        OccurredAtUtc = occurredAtUtc.Kind == DateTimeKind.Utc ? occurredAtUtc : occurredAtUtc.ToUniversalTime();
        ReceivedAtUtc = receivedAtUtc.Kind == DateTimeKind.Utc ? receivedAtUtc : receivedAtUtc.ToUniversalTime();
        EventType = eventType;
        Severity = severity;
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
        TraceId = traceId ?? Activity.Current?.TraceId.ToString();
        SpanId = spanId ?? Activity.Current?.SpanId.ToString();
        CorrelationId = correlationId;
        RequestId = requestId;
        Source = source;
        Tags = tags;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
}