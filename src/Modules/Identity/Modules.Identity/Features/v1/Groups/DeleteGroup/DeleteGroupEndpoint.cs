using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Groups.DeleteGroup;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Groups.DeleteGroup;

public static class DeleteGroupEndpoint
{
    public static RouteHandlerBuilder MapDeleteGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        // TODO: Return TypedResults.NoContent() once the Blazor NSwag client is regenerated
        // with a config that maps this to Task (void) instead of Task<Unit>.
        // Currently the generated GroupsDeleteAsync returns Task<Unit> and tries to
        // deserialize the empty 204 body, throwing a deserialization exception.
        return endpoints.MapDelete("/groups/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteGroupCommand(id), cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("DeleteGroup")
        .WithSummary("Delete a group")
        .RequirePermission(IdentityPermissionConstants.Groups.Delete)
        .WithDescription("Soft delete a group. System groups cannot be deleted.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
