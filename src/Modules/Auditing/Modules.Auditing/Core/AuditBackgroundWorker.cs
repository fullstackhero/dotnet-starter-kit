using System.Threading.Channels;
using FSH.Modules.Auditing.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Auditing;

/// <summary>
/// Drains the channel and writes to the configured sink in batches.
/// </summary>
public sealed class AuditBackgroundWorker : BackgroundService
{
    private readonly ChannelAuditPublisher _publisher;
    private readonly IAuditSink _sink;
    private readonly ILogger<AuditBackgroundWorker> _logger;

    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;

    public AuditBackgroundWorker(
        ChannelAuditPublisher publisher,
        IAuditSink sink,
        ILogger<AuditBackgroundWorker> logger,
        int batchSize = 200,
        int flushIntervalMs = 1000)
    {
        _publisher = publisher;
        _sink = sink;
        _logger = logger;
        _batchSize = Math.Max(1, batchSize);
        _flushInterval = TimeSpan.FromMilliseconds(Math.Max(50, flushIntervalMs));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _publisher.Reader;
        var batch = new List<AuditEnvelope>(_batchSize);
        var delayTask = Task.Delay(_flushInterval, stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var (shouldContinue, newDelayTask) = await ProcessBatchCycleAsync(reader, batch, delayTask, stoppingToken);
                delayTask = newDelayTask;

                if (!shouldContinue)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit background worker crashed.");
        }

        await FinalFlushAsync(batch, stoppingToken);
    }

    private async Task<(bool shouldContinue, Task delayTask)> ProcessBatchCycleAsync(
        ChannelReader<AuditEnvelope> reader,
        List<AuditEnvelope> batch,
        Task delayTask,
        CancellationToken stoppingToken)
    {
        DrainAvailableItems(reader, batch);

        if (batch.Count >= _batchSize)
        {
            await FlushAsync(batch, stoppingToken);
            return (true, Task.Delay(_flushInterval, stoppingToken));
        }

        var readTask = reader.WaitToReadAsync(stoppingToken).AsTask();
        var winner = await Task.WhenAny(readTask, delayTask);

        if (winner == readTask)
        {
            var canRead = await readTask.ConfigureAwait(false);
            return (canRead, delayTask);
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
                await _sink.WriteAsync(batch, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Final audit flush failed.");
            }
        }
    }

    private async Task FlushAsync(List<AuditEnvelope> batch, CancellationToken ct)
    {
        try
        {
            await _sink.WriteAsync(batch, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit background flush failed.");
            await Task.Delay(250, ct);
        }
        finally
        {
            batch.Clear();
        }
    }
}

