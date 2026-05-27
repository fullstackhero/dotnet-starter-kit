using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Contracts.v1.Usage;
using FSH.Modules.Billing.Services;
using Mediator;

namespace FSH.Modules.Billing.Features.v1.Usage.CaptureUsageSnapshots;

public sealed class CaptureUsageSnapshotsCommandHandler(IUsageReporter reporter)
    : ICommandHandler<CaptureUsageSnapshotsCommand, IReadOnlyList<UsageSnapshotDto>>
{
    public async ValueTask<IReadOnlyList<UsageSnapshotDto>> Handle(
        CaptureUsageSnapshotsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var snapshots = await reporter
            .CaptureForPeriodAsync(command.TenantId, command.PeriodYear, command.PeriodMonth, cancellationToken)
            .ConfigureAwait(false);

        return snapshots
            .Select(s => new UsageSnapshotDto(
                s.Id,
                s.TenantId,
                s.PeriodYear,
                s.PeriodMonth,
                s.Resource,
                s.UsedUnits,
                s.LimitUnits,
                s.Overage,
                s.CapturedAtUtc))
            .ToList();
    }
}
