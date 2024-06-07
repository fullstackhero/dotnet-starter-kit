using FSH.Framework.Core.Identity.Roles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Roles.Endpoints;

public static class DeleteRoleEndpoint
{
    public static RouteHandlerBuilder MapDeleteRoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/api/roles/{id}", async (string id, IRoleService roleService) =>
        {
            await roleService.DeleteRoleAsync(id);
            return Results.NoContent();
        })
        .WithName("DeleteRole")
        .WithSummary("Delete a role by ID")
        .WithDescription("Remove a role from the system by its ID.");
    }
}
