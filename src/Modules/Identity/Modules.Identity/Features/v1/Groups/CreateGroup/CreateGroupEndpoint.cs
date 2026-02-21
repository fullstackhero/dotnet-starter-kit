using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Groups.CreateGroup;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Groups.CreateGroup;

public static class CreateGroupEndpoint
{
    public static RouteHandlerBuilder MapCreateGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/groups", async (IMediator mediator, [FromBody] CreateGroupCommand request, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(request, cancellationToken)))
        .WithName("CreateGroup")
        .WithSummary("Create a new group")
        .RequirePermission(IdentityPermissionConstants.Groups.Create)
        .WithDescription("Create a new group with optional role assignments.")
        .Produces<GroupDto>(StatusCodes.Status200OK);
    }
}
