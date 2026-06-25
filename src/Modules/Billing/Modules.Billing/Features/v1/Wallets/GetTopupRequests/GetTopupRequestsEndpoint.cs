using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.Authorization;
using FSH.Modules.Billing.Contracts.v1.Wallets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Billing.Features.v1.Wallets.GetTopupRequests;

public static class GetTopupRequestsEndpoint
{
    internal static RouteHandlerBuilder MapGetTopupRequestsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/wallet/topup-requests",
                (string? tenantId, TopupRequestStatus? status, int pageNumber, int pageSize,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetTopupRequestsQuery(
                        tenantId,
                        status,
                        pageNumber <= 0 ? 1 : pageNumber,
                        pageSize <= 0 ? 20 : Math.Min(pageSize, 100)), ct))
            .WithName("GetTopupRequests")
            .WithSummary("List top-up requests across all tenants (operator admin)")
            .RequirePermission(BillingPermissions.View);
    }
}
