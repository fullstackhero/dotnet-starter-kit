using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Contracts.v1.Usage;
using FSH.Modules.Billing.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Usage.GetUsageSnapshots;

public sealed class GetUsageSnapshotsQueryHandler(BillingDbContext dbContext)
    : IQueryHandler<GetUsageSnapshotsQuery, IReadOnlyList<UsageSnapshotDto>>
{
    public async ValueTask<IReadOnlyList<UsageSnapshotDto>> Handle(GetUsageSnapshotsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.UsageSnapshots.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            q = q.Where(s => s.TenantId == query.TenantId);
        }
        if (query.PeriodYear is not null)
        {
            q = q.Where(s => s.PeriodYear == query.PeriodYear);
        }
        if (query.PeriodMonth is not null)
        {
            q = q.Where(s => s.PeriodMonth == query.PeriodMonth);
        }

        var snaps = await q
            .OrderByDescending(s => s.PeriodYear).ThenByDescending(s => s.PeriodMonth)
            .ThenBy(s => s.TenantId).ThenBy(s => s.Resource)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return snaps
            .Select(s => new UsageSnapshotDto(s.Id, s.TenantId, s.PeriodYear, s.PeriodMonth, s.Resource, s.UsedUnits, s.LimitUnits, s.Overage, s.CapturedAtUtc))
            .ToList();
    }
}
