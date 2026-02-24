using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Groups.UpdateGroup;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Groups.UpdateGroup;

public static class UpdateGroupEndpoint
{
    public static RouteHandlerBuilder MapUpdateGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/groups/{id:guid}", async (Guid id, IMediator mediator, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(new UpdateGroupCommand(id, request.Name, request.Description, request.IsDefault, request.RoleIds), cancellationToken)))
        .WithName("UpdateGroup")
        .WithSummary("Update a group")
        .RequirePermission(IdentityPermissionConstants.Groups.Update)
        .WithDescription("Update a group's name, description, default status, and role assignments.")
        .Produces<GroupDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }
}

public sealed record UpdateGroupRequest(
    string Name,
    string? Description,
    bool IsDefault,
    IReadOnlyList<string>? RoleIds);
