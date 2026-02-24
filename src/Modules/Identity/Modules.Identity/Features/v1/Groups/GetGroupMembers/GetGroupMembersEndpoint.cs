using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Groups.GetGroupMembers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Groups.GetGroupMembers;

public static class GetGroupMembersEndpoint
{
    public static RouteHandlerBuilder MapGetGroupMembersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/groups/{groupId:guid}/members", async (Guid groupId, IMediator mediator, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(new GetGroupMembersQuery(groupId), cancellationToken)))
        .WithName("GetGroupMembers")
        .WithSummary("Get members of a group")
        .RequirePermission(IdentityPermissionConstants.Groups.View)
        .WithDescription("Retrieve all users that belong to a specific group.")
        .Produces<IEnumerable<GroupMemberDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}
