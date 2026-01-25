using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Roles.UpdatePermissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;

public static class UpdateRolePermissionsEndpoint
{
    public static RouteHandlerBuilder MapUpdateRolePermissionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{id}/permissions", Handler)
        .WithName("UpdateRolePermissions")
        .WithSummary("Update role permissions")
        .RequirePermission(IdentityPermissionConstants.Roles.Update)
        .WithDescription("Replace the set of permissions assigned to a role.");
    }

    private static async Task<Results<Ok<string>, BadRequest>> Handler(
        string id,
        [FromBody] UpdatePermissionsCommand request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (id != request.RoleId)
        {
            return TypedResults.BadRequest();
        }

        var response = await mediator.Send(request, cancellationToken);
        return TypedResults.Ok(response);
    }
}
