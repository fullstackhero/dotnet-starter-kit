using FSH.Modules.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Services;

/// <summary>
/// Background service that periodically cleans up expired sessions.
/// Runs every hour and removes sessions that have been expired for more than 30 days.
/// </summary>
public sealed class SessionCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionCleanupHostedService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly int _retentionDays = 30;

    public SessionCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionCleanupHostedService> logger,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                await CleanupExpiredSessionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                // Cleanup loop must not crash the host — failures are retried on the next interval
                _logger.LogError(ex, "Error during session cleanup");
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var cutoffDate = now.AddDays(-_retentionDays);
        var deleted = await db.UserSessions
            .Where(s => s.ExpiresAt < now && s.ExpiresAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0 && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", deleted);
        }
    }
}