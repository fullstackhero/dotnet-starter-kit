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
        return endpoints.MapDelete("/roles/{id:guid}", async (string id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new DeleteRoleCommand(id), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("DeleteRole")
        .WithSummary("Delete role by ID")
        .RequirePermission(IdentityPermissionConstants.Roles.Delete)
        .WithDescription("Remove an existing role by its unique identifier.");
    }
}
