using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public static class RemoveUserFromGroupEndpoint
{
    public static RouteHandlerBuilder MapRemoveUserFromGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        // TODO: Return TypedResults.NoContent() once the Blazor NSwag client is regenerated
        // with a config that maps this to Task (void) instead of Task<Unit>.
        // Currently the generated RemoveUserFromGroupAsync returns Task<Unit> and tries to
        // deserialize the empty 204 body, throwing a deserialization exception.
        return endpoints.MapDelete("/groups/{groupId:guid}/members/{userId}", async (Guid groupId, string userId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new RemoveUserFromGroupCommand(groupId, userId), cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("RemoveUserFromGroup")
        .WithSummary("Remove a user from a group")
        .RequirePermission(IdentityPermissionConstants.Groups.ManageMembers)
        .WithDescription("Remove a specific user from a group.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
