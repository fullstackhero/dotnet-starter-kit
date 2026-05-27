namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Destination for audit events (e.g., SQL, file, OTLP). Implementations must be efficient and batch-friendly.
/// </summary>
public interface IAuditSink
{
    Task WriteAsync(IReadOnlyList<AuditEnvelope> batch, CancellationToken ct);
}