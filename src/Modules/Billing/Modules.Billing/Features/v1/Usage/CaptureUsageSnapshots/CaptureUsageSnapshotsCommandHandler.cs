using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Contracts.v1.Usage;
using FSH.Modules.Billing.Services;
using Mediator;

namespace FSH.Modules.Billing.Features.v1.Usage.CaptureUsageSnapshots;

public sealed class CaptureUsageSnapshotsCommandHandler(
    IUsageReporter reporter,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<CaptureUsageSnapshotsCommand, IReadOnlyList<UsageSnapshotDto>>
{
    public async ValueTask<IReadOnlyList<UsageSnapshotDto>> Handle(
        CaptureUsageSnapshotsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Only the root operator may capture usage for an arbitrary tenant; a tenant caller is pinned
        // to its own tenant so it can't fabricate another tenant's usage/overage snapshots.
        var callerTenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.")
            {
                MessageKey = "Error.TenantContextRequired",
            };
        var isRoot = callerTenantId == MultitenancyConstants.Root.Id;
        var targetTenantId = isRoot ? command.TenantId : callerTenantId;

        var snapshots = await reporter
            .CaptureForPeriodAsync(targetTenantId, command.PeriodYear, command.PeriodMonth, cancellationToken)
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
