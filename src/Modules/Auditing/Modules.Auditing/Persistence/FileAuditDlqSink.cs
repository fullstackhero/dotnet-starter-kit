using FSH.Modules.Auditing.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Writes dead-lettered audit envelopes as JSONL to a daily-rotated file
/// under <c>{ContentRoot}/audit-dlq/audit-dlq-{yyyy-MM-dd}.jsonl</c>.
///
/// File-based deliberately: it has no dependency on Postgres, Redis, or
/// any other infrastructure that might be the reason the primary sink
/// failed in the first place. Operators are expected to ship the file
/// off-host (Filebeat, Vector, etc.) and replay into the warehouse out
/// of band.
/// </summary>
public sealed class FileAuditDlqSink : IAuditDlqSink, IDisposable
{
    private readonly string _directory;
    private readonly IAuditSerializer _serializer;
    private readonly ILogger<FileAuditDlqSink> _log;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public FileAuditDlqSink(
        IHostEnvironment env,
        IAuditSerializer serializer,
        ILogger<FileAuditDlqSink> log,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(env);
        _serializer = serializer;
        _log = log;
        _timeProvider = timeProvider;
        _directory = Path.Combine(env.ContentRootPath, "audit-dlq");
    }

    public void Dispose() => _writeGate.Dispose();

    public async Task WriteAsync(IReadOnlyList<AuditEnvelope> batch, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Count == 0) return;

        try
        {
            Directory.CreateDirectory(_directory);
            var date = _timeProvider.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var path = Path.Combine(_directory, $"audit-dlq-{date}.jsonl");

            // Build the lines off the gate so contention only covers the I/O.
            var sb = new StringBuilder(capacity: batch.Count * 256);
            foreach (var envelope in batch)
            {
                sb.Append(SerializeRecord(envelope)).Append('\n');
            }
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            await _writeGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await using var fs = new FileStream(
                    path,
                    new FileStreamOptions
                    {
                        Mode = FileMode.Append,
                        Access = FileAccess.Write,
                        Share = FileShare.Read,
                        Options = FileOptions.Asynchronous,
                    });
                await fs.WriteAsync(bytes, ct).ConfigureAwait(false);
            }
            finally
            {
                _writeGate.Release();
            }

            AuditingTelemetry.DeadLettered.Add(batch.Count);
            _log.LogWarning("Dead-lettered {Count} audit events to {Path}.", batch.Count, path);
        }
        catch (Exception ex)
        {
            // DLQ is the last line of defence. If it fails too, just log and do not rethrow —
            // the worker has nowhere to escalate.
            _log.LogError(ex, "Audit DLQ write failed; {Count} events lost.", batch.Count);
        }
    }

    private string SerializeRecord(AuditEnvelope envelope)
    {
        var record = new
        {
            envelope.Id,
            envelope.OccurredAtUtc,
            envelope.ReceivedAtUtc,
            EventType = envelope.EventType.ToString(),
            Severity = envelope.Severity.ToString(),
            envelope.TenantId,
            envelope.UserId,
            envelope.UserName,
            envelope.TraceId,
            envelope.SpanId,
            envelope.CorrelationId,
            envelope.RequestId,
            envelope.Source,
            Tags = envelope.Tags.ToString(),
            Payload = _serializer.SerializePayload(envelope.Payload),
        };
        return JsonSerializer.Serialize(record);
    }
}
