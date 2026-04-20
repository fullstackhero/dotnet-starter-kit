using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Billing.Services;

public sealed class UsageReporter : IUsageReporter
{
    private readonly BillingDbContext _db;
    private readonly IQuotaService _quotas;
    private readonly QuotaPlanResolver _planResolver;
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly ILogger<UsageReporter> _logger;

    public UsageReporter(
        BillingDbContext db,
        IQuotaService quotas,
        QuotaPlanResolver planResolver,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        ILogger<UsageReporter> logger)
    {
        _db = db;
        _quotas = quotas;
        _planResolver = planResolver;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UsageSnapshot>> CaptureForPeriodAsync(
        string tenantId,
        int periodYear,
        int periodMonth,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var tenant = await _tenantStore.GetAsync(tenantId).ConfigureAwait(false);
        var existing = await _db.UsageSnapshots
            .Where(s => s.TenantId == tenantId && s.PeriodYear == periodYear && s.PeriodMonth == periodMonth)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var snapshots = new List<UsageSnapshot>(capacity: 4);
        foreach (var resource in Enum.GetValues<QuotaResource>())
        {
            var already = existing.FirstOrDefault(s => s.Resource == resource);
            if (already is not null)
            {
                snapshots.Add(already);
                continue;
            }

            var used = await _quotas.GetCurrentAsync(tenantId, resource, cancellationToken).ConfigureAwait(false);
            var limit = _planResolver.ResolveLimit(tenant, resource);
            var snap = UsageSnapshot.Capture(tenantId, periodYear, periodMonth, resource, used, limit);
            _db.UsageSnapshots.Add(snap);
            snapshots.Add(snap);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("[Billing] captured {Count} usage snapshots for tenant {TenantId} period {Year}-{Month:00}",
            snapshots.Count, tenantId, periodYear, periodMonth);
        return snapshots;
    }
}
