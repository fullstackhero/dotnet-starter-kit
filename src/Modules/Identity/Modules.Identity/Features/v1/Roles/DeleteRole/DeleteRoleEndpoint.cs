using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Roles.DeleteRole;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Roles.DeleteRole;

public static class DeleteRoleEndpoint
{
    public static RouteHandlerBuilder MapDeleteRoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        // TODO: Return TypedResults.NoContent() once the Blazor NSwag client is regenerated
        // with the original configuration that preserves existing interface contracts.
        // Currently the generated RolesDeleteAsync only handles HTTP 200.
        return endpoints.MapDelete("/roles/{id:guid}", async (string id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteRoleCommand(id), cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("DeleteRole")
        .WithSummary("Delete role by ID")
        .RequirePermission(IdentityPermissionConstants.Roles.Delete)
        .WithDescription("Remove an existing role by its unique identifier.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
