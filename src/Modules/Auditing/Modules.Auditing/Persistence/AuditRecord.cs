namespace FSH.Modules.Auditing;

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }

    public int EventType { get; set; }
    public byte Severity { get; set; }

    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? Source { get; set; }

    public long Tags { get; set; }

    public string PayloadJson { get; set; } = default!;
}