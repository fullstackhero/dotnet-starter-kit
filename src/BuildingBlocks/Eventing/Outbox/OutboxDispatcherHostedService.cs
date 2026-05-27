using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// Background service that periodically dispatches outbox messages.
/// Alternative to Hangfire-based scheduling for simpler deployments.
/// </summary>
public sealed partial class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;
    private readonly TimeSpan _interval;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<EventingOptions> options,
        ILogger<OutboxDispatcherHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(options.Value.OutboxDispatchIntervalSeconds > 0
            ? options.Value.OutboxDispatchIntervalSeconds
            : 10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarted(_interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchOutboxAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            // Broad catch is intentional: the hosted service loop must not crash
            // due to transient errors; failures are logged and the next cycle retries.
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching outbox messages");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox dispatcher hosted service stopped");
    }

    private async Task DispatchOutboxAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        await dispatcher.DispatchAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox dispatcher hosted service started. Dispatch interval: {Interval}s")]
    private partial void LogServiceStarted(double interval);
}