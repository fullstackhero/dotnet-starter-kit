using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Daily Hangfire job that prunes the audit table per
/// <see cref="AuditRetentionOptions"/>, in every tenant. Uses <c>ExecuteDeleteAsync</c> with
/// a bounded batch size so a single run doesn't take a long-held lock on
/// the table — each event-type sweep loops until fewer than batch-size
/// rows are deleted.
/// </summary>
public sealed class AuditRetentionJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditRetentionOptions _opts;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditRetentionJob> _logger;

    public AuditRetentionJob(
        IServiceScopeFactory scopeFactory,
        AuditRetentionOptions opts,
        TimeProvider timeProvider,
        ILogger<AuditRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _opts = opts;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (!_opts.Enabled)
        {
            _logger.LogInformation("[Auditing] retention job skipped (Enabled=false).");
            return;
        }

        // The recurring job is registered without a tenant, so none is resolved and the default-on
        // tenant filter on AuditRecords has nothing to compare against (it throws). Each tenant is
        // purged inside its own context, which also points AuditDbContext at a tenant's dedicated
        // database when it has one.
        List<AppTenantInfo> tenants;
        using (var scope = _scopeFactory.CreateScope())
        {
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
            tenants = (await tenantStore.GetAllAsync().ConfigureAwait(false)).ToList();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        long total = 0;
        foreach (var tenant in tenants)
        {
            ct.ThrowIfCancellationRequested();
            total += await PurgeTenantAsync(tenant, now, ct).ConfigureAwait(false);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Auditing] retention job purged {Total} rows across {TenantCount} tenants.",
                total, tenants.Count);
        }
    }

    private async Task<long> PurgeTenantAsync(AppTenantInfo tenant, DateTime now, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            long total = 0;
            total += await SweepAsync(db, tenant.Id, AuditEventType.Activity, now.AddDays(-_opts.ActivityRetentionDays), ct).ConfigureAwait(false);
            total += await SweepAsync(db, tenant.Id, AuditEventType.EntityChange, now.AddDays(-_opts.EntityChangeRetentionDays), ct).ConfigureAwait(false);
            total += await SweepAsync(db, tenant.Id, AuditEventType.Security, now.AddDays(-_opts.SecurityRetentionDays), ct).ConfigureAwait(false);
            total += await SweepAsync(db, tenant.Id, AuditEventType.Exception, now.AddDays(-_opts.ExceptionRetentionDays), ct).ConfigureAwait(false);
            return total;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One tenant's database being unreachable must not keep the other tenants' audit rows around.
            _logger.LogError(ex, "[Auditing] retention purge failed for tenant {TenantId}.", tenant.Id);
            return 0;
        }
    }

    private async Task<long> SweepAsync(
        AuditDbContext db, string? tenantId, AuditEventType eventType, DateTime cutoffUtc, CancellationToken ct)
    {
        long swept = 0;
        var typeId = (int)eventType;
        var batchSize = Math.Max(100, _opts.DeleteBatchSize);

        while (!ct.IsCancellationRequested)
        {
            // Sub-query trick: ExecuteDeleteAsync doesn't support TOP/LIMIT
            // directly, so we filter to a bounded id-set first.
            var deleted = await db.AuditRecords
                .Where(a => a.EventType == typeId
                    && a.OccurredAtUtc < cutoffUtc
                    && db.AuditRecords
                        .Where(b => b.EventType == typeId && b.OccurredAtUtc < cutoffUtc)
                        .OrderBy(b => b.OccurredAtUtc)
                        .Select(b => b.Id)
                        .Take(batchSize)
                        .Contains(a.Id))
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            swept += deleted;
            if (deleted < batchSize) break;
        }

        if (swept > 0 && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Auditing] purged {Count} {EventType} events older than {Cutoff:o} for tenant {TenantId}.",
                swept, eventType, cutoffUtc, tenantId);
        }
        return swept;
    }
}
