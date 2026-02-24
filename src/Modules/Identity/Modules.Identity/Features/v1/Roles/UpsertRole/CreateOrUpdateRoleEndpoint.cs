using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Roles.UpsertRole;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Roles.UpsertRole;

public static class CreateOrUpdateRoleEndpoint
{
    public static RouteHandlerBuilder MapCreateOrUpdateRoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/roles", async (IMediator mediator, [FromBody] UpsertRoleCommand request, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(request, cancellationToken)))
        .WithName("CreateOrUpdateRole")
        .WithSummary("Create or update role")
        .RequirePermission(IdentityPermissionConstants.Roles.Create)
        .WithDescription("Create a new role or update an existing role's name and description.")
        .Produces<RoleDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
