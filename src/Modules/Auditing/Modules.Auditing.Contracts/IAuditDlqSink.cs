namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Fallback destination for audit batches that the primary sink could not
/// persist. Implementations must be reliable and self-contained — the DLQ
/// is the last line of defence, so it cannot share infrastructure with the
/// primary sink (e.g. don't send the SQL DLQ to the same Postgres that
/// just failed). Recommended targets: local JSONL file with daily rotation,
/// or a queue with at-least-once delivery.
/// </summary>
public interface IAuditDlqSink
{
    /// <summary>
    /// Persist a batch that exhausted all primary-sink retries. The DLQ
    /// implementation is expected to never throw — log the failure and
    /// drop on the floor before raising, since the audit pipeline has
    /// nowhere left to escalate.
    /// </summary>
    Task WriteAsync(IReadOnlyList<AuditEnvelope> batch, CancellationToken ct);
}
