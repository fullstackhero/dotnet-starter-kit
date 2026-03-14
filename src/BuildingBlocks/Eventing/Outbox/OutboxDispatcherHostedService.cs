using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// Background service that periodically dispatches outbox messages.
/// Alternative to Hangfire-based scheduling for simpler deployments.
/// </summary>
public sealed class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;
    private readonly TimeSpan _interval;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<EventingOptions> options,
        ILogger<OutboxDispatcherHostedService> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        ArgumentNullException.ThrowIfNull(options);
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
        _interval = TimeSpan.FromSeconds(options.Value.OutboxDispatchIntervalSeconds > 0
            ? options.Value.OutboxDispatchIntervalSeconds
            : 10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to be fully started to ensure migrations and startup tasks are complete.
        // This prevents the dispatcher from attempting to poll tables that don't exist yet, 
        // which avoids generating noisy EF Core database command errors in the logs.
        if (!await WaitForAppStartup(stoppingToken))
        {
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Outbox dispatcher hosted service started. Dispatch interval: {Interval}s",
                _interval.TotalSeconds);
        }

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

    private async Task<bool> WaitForAppStartup(CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        using var registration = _hostApplicationLifetime.ApplicationStarted.Register(() => tcs.TrySetResult());
        using var tokenRegistration = stoppingToken.Register(() => tcs.TrySetCanceled(stoppingToken));

        try
        {
            await tcs.Task.ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
