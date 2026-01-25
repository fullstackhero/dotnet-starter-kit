using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.DeleteUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.DeleteUser;

public static class DeleteUserEndpoint
{
    internal static RouteHandlerBuilder MapDeleteUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/users/{id:guid}", async (string id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new DeleteUserCommand(id), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("DeleteUser")
        .WithSummary("Delete user")
        .RequirePermission(IdentityPermissionConstants.Users.Delete)
        .WithDescription("Delete a user by unique identifier.");
    }
}
