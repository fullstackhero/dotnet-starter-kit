using FSH.Modules.Auditing.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Channels;

namespace FSH.Modules.Auditing;

/// <summary>
/// Drains the channel and writes to the configured sink in batches.
/// </summary>
public sealed class AuditBackgroundWorker : BackgroundService
{
    private readonly ChannelAuditPublisher _publisher;
    private readonly IAuditSink _sink;
    private readonly IAuditDlqSink _dlq;
    private readonly ILogger<AuditBackgroundWorker> _logger;

    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;

    /// <summary>Maximum primary-sink retry attempts per batch before DLQ.</summary>
    private const int MaxRetries = 3;

    /// <summary>Initial retry backoff. Doubled per attempt up to 2s ceiling.</summary>
    private static readonly TimeSpan InitialBackoff = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(2);

    public AuditBackgroundWorker(
        ChannelAuditPublisher publisher,
        IAuditSink sink,
        IAuditDlqSink dlq,
        ILogger<AuditBackgroundWorker> logger,
        int batchSize = 200,
        int flushIntervalMs = 1000)
    {
        _publisher = publisher;
        _sink = sink;
        _dlq = dlq;
        _logger = logger;
        _batchSize = Math.Max(1, batchSize);
        _flushInterval = TimeSpan.FromMilliseconds(Math.Max(50, flushIntervalMs));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditEnvelope>(_batchSize);
        var delayTask = Task.Delay(_flushInterval, stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var (shouldContinue, newDelayTask) = await ProcessBatchCycleAsync(batch, delayTask, stoppingToken);
                delayTask = newDelayTask;

                if (!shouldContinue)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during graceful shutdown
        }
        catch (Exception ex)
        {
            // Background worker must not crash the host — log and let final flush attempt to save remaining items
            _logger.LogError(ex, "Audit background worker crashed.");
        }

        await FinalFlushAsync(batch, stoppingToken);
    }

    private async Task<(bool shouldContinue, Task delayTask)> ProcessBatchCycleAsync(
        List<AuditEnvelope> batch,
        Task delayTask,
        CancellationToken stoppingToken)
    {
        // Security lane is drained first so back-pressured publishers
        // unblock as quickly as possible. Default lane fills the rest of
        // the batch so a single flush amortizes both lanes' I/O.
        DrainAvailableItems(_publisher.SecurityReader, batch);
        DrainAvailableItems(_publisher.Reader, batch);

        if (batch.Count >= _batchSize)
        {
            await FlushAsync(batch, stoppingToken);
            return (true, Task.Delay(_flushInterval, stoppingToken));
        }

        // Wait until either lane has data or the flush interval elapses.
        // Neither channel is ever closed by the publisher (the only signal
        // is stoppingToken cancellation, which we catch in ExecuteAsync),
        // so any non-delay completion means "more data is ready".
        var securityWait = _publisher.SecurityReader.WaitToReadAsync(stoppingToken).AsTask();
        var defaultWait = _publisher.Reader.WaitToReadAsync(stoppingToken).AsTask();
        var winner = await Task.WhenAny(securityWait, defaultWait, delayTask);

        if (winner == securityWait || winner == defaultWait)
        {
            return (true, delayTask);
        }

        if (batch.Count > 0)
        {
            await FlushAsync(batch, stoppingToken);
        }

        return (true, Task.Delay(_flushInterval, stoppingToken));
    }

    private void DrainAvailableItems(ChannelReader<AuditEnvelope> reader, List<AuditEnvelope> batch)
    {
        while (batch.Count < _batchSize && reader.TryRead(out var item))
        {
            batch.Add(item);
        }
    }

    private async Task FinalFlushAsync(List<AuditEnvelope> batch, CancellationToken stoppingToken)
    {
        if (batch.Count > 0 && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlushAsync(batch, stoppingToken).ConfigureAwait(false);
            }
            // Best-effort: final flush failure should not propagate during shutdown
            catch (Exception ex)
            {
                _logger.LogError(ex, "Final audit flush failed.");
            }
        }
    }

    /// <summary>
    /// Attempts to flush <paramref name="batch"/> via the primary sink with
    /// bounded exponential backoff. On exhaustion, hands the batch to the
    /// dead-letter sink so events survive a Postgres outage. Always clears
    /// <paramref name="batch"/> before returning so the caller can re-fill.
    /// </summary>
    private async Task FlushAsync(List<AuditEnvelope> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return;

        var sw = Stopwatch.StartNew();
        var snapshot = batch.ToArray();
        try
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await _sink.WriteAsync(snapshot, ct).ConfigureAwait(false);
                    AuditingTelemetry.Flushed.Add(snapshot.Length);
                    return;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Expected during shutdown — don't retry, don't DLQ. Items
                    // are lost during forced termination; that's acceptable.
                    return;
                }
                catch (Exception ex)
                {
                    AuditingTelemetry.FlushFailed.Add(1,
                        new KeyValuePair<string, object?>("attempt", attempt));

                    if (attempt == MaxRetries)
                    {
                        _logger.LogError(ex, "Audit sink failed after {Attempts} attempts; sending {Count} events to DLQ.",
                            attempt, snapshot.Length);
                        await _dlq.WriteAsync(snapshot, ct).ConfigureAwait(false);
                        return;
                    }

                    _logger.LogWarning(ex,
                        "Audit sink flush attempt {Attempt}/{Max} failed; retrying.",
                        attempt, MaxRetries);

                    var backoff = ComputeBackoff(attempt);
                    try { await Task.Delay(backoff, ct).ConfigureAwait(false); }
                    catch (OperationCanceledException) { return; }
                }
            }
        }
        finally
        {
            batch.Clear();
            AuditingTelemetry.FlushDurationMs.Record(sw.Elapsed.TotalMilliseconds);
        }
    }

    private static TimeSpan ComputeBackoff(int attempt)
    {
        var ms = Math.Min(
            MaxBackoff.TotalMilliseconds,
            InitialBackoff.TotalMilliseconds * Math.Pow(2, attempt - 1));
        return TimeSpan.FromMilliseconds(ms);
    }
}
