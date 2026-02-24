using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAuditsByCorrelation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Auditing.Features.v1.GetAuditsByCorrelation;

public static class GetAuditsByCorrelationEndpoint
{
    public static RouteHandlerBuilder MapGetAuditsByCorrelationEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet(
                "/by-correlation/{correlationId}",
                async (string correlationId, DateTime? fromUtc, DateTime? toUtc, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetAuditsByCorrelationQuery
                    {
                        CorrelationId = correlationId,
                        FromUtc = fromUtc,
                        ToUtc = toUtc
                    }, cancellationToken)))
            .WithName("GetAuditsByCorrelation")
            .WithSummary("Get audit events by correlation id")
            .WithDescription("Retrieve audit events associated with a given correlation id.")
            .RequirePermission(AuditingPermissionConstants.View)
            .Produces<IEnumerable<AuditSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}

