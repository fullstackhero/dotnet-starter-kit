using FSH.Modules.Auditing.Contracts;
using System.Text.Json;

namespace FSH.Modules.Auditing.Contracts.Dtos;

public sealed class AuditDetailDto
{
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public AuditEventType EventType { get; set; }

    public AuditSeverity Severity { get; set; }

    public string? TenantId { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? CorrelationId { get; set; }

    public string? RequestId { get; set; }

    public string? Source { get; set; }

    public AuditTag Tags { get; set; }

    /// <summary>
    /// Masked, deserialized payload. Serialized back to JSON for clients.
    /// </summary>
    public JsonElement Payload { get; set; }
}