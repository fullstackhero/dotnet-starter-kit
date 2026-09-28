using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Services;

/// <summary>
/// Background service that periodically cleans up expired sessions.
/// Runs every hour and, in every tenant, removes sessions that have been expired for more than 30 days.
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
                await Task.Delay(_cleanupInterval, stoppingToken).ConfigureAwait(false);
                await CleanupExpiredSessionsAsync(stoppingToken).ConfigureAwait(false);
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

    internal async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        // A hosted service has no request, so no tenant is resolved and the default-on tenant filter on
        // UserSessions has nothing to compare against. Each tenant is cleaned inside its own context,
        // which also points IdentityDbContext at a tenant's dedicated database when it has one.
        List<AppTenantInfo> tenants;
        using (var scope = _scopeFactory.CreateScope())
        {
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
            tenants = (await tenantStore.GetAllAsync().ConfigureAwait(false)).ToList();
        }

        // cutoffDate = now - retentionDays, so ExpiresAt < cutoffDate already implies ExpiresAt < now.
        var cutoffDate = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-_retentionDays);
        foreach (var tenant in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await CleanupTenantSessionsAsync(tenant, cutoffDate, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CleanupTenantSessionsAsync(AppTenantInfo tenant, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var deleted = await db.UserSessions
                .Where(s => s.ExpiresAt < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted > 0 && _logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Cleaned up {Count} expired sessions for tenant {TenantId}", deleted, tenant.Id);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One tenant's database being unreachable must not keep the other tenants' sessions around.
            _logger.LogError(ex, "Session cleanup failed for tenant {TenantId}", tenant.Id);
        }
    }
}