using System.Diagnostics;

namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Concrete event instance ready to be published/persisted.
/// Carries normalized metadata + strongly-typed Payload.
/// </summary>
public sealed class AuditEnvelope(
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
    object payload) : IAuditEvent
{
    public Guid Id { get; } = id;
    public DateTime OccurredAtUtc { get; } = occurredAtUtc.Kind == DateTimeKind.Utc ? occurredAtUtc : occurredAtUtc.ToUniversalTime();
    public DateTime ReceivedAtUtc { get; } = receivedAtUtc.Kind == DateTimeKind.Utc ? receivedAtUtc : receivedAtUtc.ToUniversalTime();

    public AuditEventType EventType { get; } = eventType;
    public AuditSeverity Severity { get; } = severity;

    public string? TenantId { get; } = tenantId;
    public string? UserId { get; } = userId;
    public string? UserName { get; } = userName;

    public string? TraceId { get; } = traceId ?? Activity.Current?.TraceId.ToString();
    public string? SpanId { get; } = spanId ?? Activity.Current?.SpanId.ToString();
    public string? CorrelationId { get; } = correlationId;
    public string? RequestId { get; } = requestId;
    public string? Source { get; } = source;

    public AuditTag Tags { get; } = tags;

    public object Payload { get; } = payload ?? throw new ArgumentNullException(nameof(payload));
}