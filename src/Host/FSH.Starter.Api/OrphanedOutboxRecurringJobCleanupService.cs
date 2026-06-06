using Hangfire;
using Hangfire.Storage;

namespace FSH.Starter.Api;

/// <summary>
/// One-shot, best-effort removal of orphaned <c>{module}-outbox-dispatcher</c> Hangfire recurring jobs.
/// The outbox is now dispatched by the framework's <c>OutboxDispatcherHostedService</c> (on by default);
/// the per-module Hangfire recurring jobs were retired (commit 66130fc6), but Hangfire persists recurring
/// jobs in storage, so deployments created on an older build keep firing them every minute — racing the
/// hosted service over the same rows and flooding the logs. Removing the code did not delete the stored
/// schedule; this service self-heals such deployments on next boot. Safe to keep: a no-op once clean.
/// Runs as a <see cref="BackgroundService"/> so it never blocks startup.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by DI via AddHostedService")]
internal sealed class OrphanedOutboxRecurringJobCleanupService : BackgroundService
{
    private const string OrphanSuffix = "-outbox-dispatcher";

    private readonly JobStorage _storage;
    private readonly IRecurringJobManager _recurringJobs;
    private readonly ILogger<OrphanedOutboxRecurringJobCleanupService> _logger;

    public OrphanedOutboxRecurringJobCleanupService(
        JobStorage storage,
        IRecurringJobManager recurringJobs,
        ILogger<OrphanedOutboxRecurringJobCleanupService> logger)
    {
        _storage = storage;
        _recurringJobs = recurringJobs;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let Hangfire finish initializing its schema before enumerating recurring jobs.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);

        try
        {
            using var connection = _storage.GetConnection();
            var orphanIds = connection.GetRecurringJobs()
                .Select(job => job.Id)
                .Where(id => id.EndsWith(OrphanSuffix, StringComparison.Ordinal))
                .ToList();

            foreach (var id in orphanIds)
            {
                _recurringJobs.RemoveIfExists(id);
                _logger.LogWarning(
                    "Removed orphaned outbox recurring job {RecurringJobId}; the outbox is dispatched by OutboxDispatcherHostedService.",
                    id);
            }
        }
        // Best-effort cleanup: storage may not be ready on first boot, or the DB may be temporarily unreachable.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Could not remove orphaned outbox recurring jobs (Hangfire storage may not be ready).");
        }
    }
}
