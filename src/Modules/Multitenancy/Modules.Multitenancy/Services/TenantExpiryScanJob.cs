using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.Events;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Multitenancy.Services;

/// <summary>
/// Daily scan that notifies tenants approaching or past their <c>ValidUpto</c>. For each active,
/// non-root tenant it classifies the state (nearing expiry / in grace / expired), records a dedup row
/// in <see cref="TenantExpiryNotice"/> (one per tenant+state+validity period), and publishes the
/// matching integration event. Notification side-effects (email) are handled by event consumers.
/// </summary>
public sealed class TenantExpiryScanJob
{
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly TenantDbContext _db;
    private readonly IEventBus _eventBus;
    private readonly IMultiTenantContextSetter _tenantContextSetter;
    private readonly TimeProvider _timeProvider;
    private readonly TenantBillingOptions _options;
    private readonly ILogger<TenantExpiryScanJob> _logger;

    public TenantExpiryScanJob(
        IMultiTenantStore<AppTenantInfo> tenantStore,
        TenantDbContext db,
        IEventBus eventBus,
        IMultiTenantContextSetter tenantContextSetter,
        TimeProvider timeProvider,
        IOptions<TenantBillingOptions> options,
        ILogger<TenantExpiryScanJob> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _tenantStore = tenantStore;
        _db = db;
        _eventBus = eventBus;
        _tenantContextSetter = tenantContextSetter;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tenants = await _tenantStore.GetAllAsync().ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var published = 0;
        foreach (var tenant in tenants)
        {
            if (!tenant.IsActive ||
                string.Equals(tenant.Id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                if (await TryNotifyAsync(tenant, now, cancellationToken).ConfigureAwait(false))
                {
                    published++;
                }
            }
#pragma warning disable CA1031 // One tenant's failure must not block the rest of the scan
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "[Multitenancy] expiry scan failed for tenant {TenantId}", tenant.Id);
            }
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Multitenancy] expiry scan published {Count} notice(s)", published);
        }
    }

    private async Task<bool> TryNotifyAsync(AppTenantInfo tenant, DateTime now, CancellationToken ct)
    {
        var validUpto = tenant.ValidUpto;
        var graceEnds = validUpto.AddDays(_options.GracePeriodDays);

        string noticeType;
        if (now > graceEnds)
        {
            noticeType = TenantExpiryNoticeTypes.Expired;
        }
        else if (now > validUpto)
        {
            noticeType = TenantExpiryNoticeTypes.EnteredGrace;
        }
        else if (now >= validUpto.AddDays(-_options.ExpiryNotificationLeadDays))
        {
            noticeType = TenantExpiryNoticeTypes.NearingExpiry;
        }
        else
        {
            return false; // healthy and outside the reminder window
        }

        // Dedup: one notice per tenant per state per validity period (re-arms when ValidUpto changes).
        var alreadyNotified = await _db.TenantExpiryNotices
            .AnyAsync(x => x.TenantId == tenant.Id && x.NoticeType == noticeType && x.ValidUptoUtc == validUpto, ct)
            .ConfigureAwait(false);
        if (alreadyNotified)
        {
            return false;
        }

        _db.TenantExpiryNotices.Add(TenantExpiryNotice.Record(tenant.Id, noticeType, validUpto, now));
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Install the Finbuckle context before publishing: downstream handlers (e.g. webhook fan-out) use
        // tenant-filtered DbContexts that NRE without it, since a background job carries no HTTP request.
        _tenantContextSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        await _eventBus.PublishAsync(BuildEvent(noticeType, tenant, validUpto, graceEnds, now), ct).ConfigureAwait(false);
        return true;
    }

    private static IIntegrationEvent BuildEvent(
        string noticeType, AppTenantInfo tenant, DateTime validUpto, DateTime graceEnds, DateTime now)
    {
        var id = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        const string source = "Multitenancy";
        var name = tenant.Name ?? tenant.Id;
        var email = tenant.AdminEmail;

        return noticeType switch
        {
            TenantExpiryNoticeTypes.NearingExpiry => new TenantNearingExpiryIntegrationEvent(
                id, now, tenant.Id, correlationId, source, name, email, tenant.Plan, validUpto, graceEnds,
                DaysRemaining: Math.Max(0, (int)Math.Ceiling((validUpto - now).TotalDays))),
            TenantExpiryNoticeTypes.EnteredGrace => new TenantEnteredGraceIntegrationEvent(
                id, now, tenant.Id, correlationId, source, name, email, tenant.Plan, validUpto, graceEnds),
            _ => new TenantExpiredIntegrationEvent(
                id, now, tenant.Id, correlationId, source, name, email, tenant.Plan, validUpto, graceEnds),
        };
    }
}
