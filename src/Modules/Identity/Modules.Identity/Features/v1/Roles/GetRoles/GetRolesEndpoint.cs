using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Roles.GetRoles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Roles.GetRoles;

public static class GetRolesEndpoint
{
    public static RouteHandlerBuilder MapGetRolesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/roles",
            async ([AsParameters] GetRolesQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(query, cancellationToken)))
        .WithName("ListRoles")
        .WithSummary("List roles (paged)")
        .RequirePermission(IdentityPermissions.Roles.View)
        .Produces<PagedResponse<RoleDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithDescription("Retrieve roles available for the current tenant. Pageable via PageNumber/PageSize; filterable via Search (case-insensitive substring against name + description).");
    }
}
