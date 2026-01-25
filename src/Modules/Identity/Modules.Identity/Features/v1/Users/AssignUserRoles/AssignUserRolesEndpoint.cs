using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.AssignUserRoles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.AssignUserRoles;

public static class AssignUserRolesEndpoint
{
    internal static RouteHandlerBuilder MapAssignUserRolesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/users/{id:guid}/roles", async (
            string id,
            AssignUserRolesCommand command,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (!string.Equals(id, command.UserId, StringComparison.Ordinal))
            {
                return Results.BadRequest();
            }

            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("AssignUserRoles")
        .WithSummary("Assign roles to user")
        .WithDescription("Assign one or more roles to a user.")
        .RequirePermission(IdentityPermissionConstants.Users.ManageRoles);
    }
}
