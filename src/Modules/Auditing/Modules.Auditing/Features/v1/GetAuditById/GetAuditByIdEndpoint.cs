using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAuditById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Auditing.Features.v1.GetAuditById;

public static class GetAuditByIdEndpoint
{
    public static RouteHandlerBuilder MapGetAuditByIdEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetAuditByIdQuery(id), cancellationToken)))
            .WithName("GetAuditById")
            .WithSummary("Get audit event by ID")
            .WithDescription("Retrieve full details for a single audit event.")
            .RequirePermission(AuditingPermissionConstants.View)
            .Produces<AuditDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}

