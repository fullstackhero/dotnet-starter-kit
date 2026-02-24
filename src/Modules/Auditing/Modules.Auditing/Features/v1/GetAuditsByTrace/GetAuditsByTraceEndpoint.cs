using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAuditsByTrace;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Auditing.Features.v1.GetAuditsByTrace;

public static class GetAuditsByTraceEndpoint
{
    public static RouteHandlerBuilder MapGetAuditsByTraceEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet(
                "/by-trace/{traceId}",
                async (string traceId, DateTime? fromUtc, DateTime? toUtc, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetAuditsByTraceQuery
                    {
                        TraceId = traceId,
                        FromUtc = fromUtc,
                        ToUtc = toUtc
                    }, cancellationToken)))
            .WithName("GetAuditsByTrace")
            .WithSummary("Get audit events by trace id")
            .WithDescription("Retrieve audit events associated with a given trace id.")
            .RequirePermission(AuditingPermissionConstants.View)
            .Produces<IEnumerable<AuditSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}

