using FSH.Modules.Billing.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Billing.Contracts.v1.Usage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Billing.Features.v1.Usage.GetUsageSnapshots;

public static class GetUsageSnapshotsEndpoint
{
    internal static RouteHandlerBuilder MapGetUsageSnapshotsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/usage",
                (string? tenantId, int? periodYear, int? periodMonth, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetUsageSnapshotsQuery(tenantId, periodYear, periodMonth), ct))
            .WithName("GetUsageSnapshots")
            .WithSummary("List captured usage snapshots")
            .RequirePermission(BillingPermissions.View);
    }
}
