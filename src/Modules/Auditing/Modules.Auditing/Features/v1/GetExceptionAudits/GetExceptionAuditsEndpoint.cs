using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetExceptionAudits;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Auditing.Features.v1.GetExceptionAudits;

public static class GetExceptionAuditsEndpoint
{
    public static RouteHandlerBuilder MapGetExceptionAuditsEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet(
                "/exceptions",
                async ([AsParameters] GetExceptionAuditsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(query, cancellationToken)))
            .WithName("GetExceptionAudits")
            .WithSummary("Get exception audit events")
            .WithDescription("Retrieve audit events related to exceptions.")
            .RequirePermission(AuditingPermissionConstants.View)
            .Produces<IEnumerable<AuditSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}

