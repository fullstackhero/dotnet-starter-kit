using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Roles.Endpoints;
public static class GetRolesEndpoint
{
    public static RouteHandlerBuilder MapGetAllRolesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/roles", async (RoleManager<FshRole> roleManager) =>
        {
            var roles = await roleManager.Roles.ToListAsync();
            return Results.Ok(roles);
        })
        .WithName("GetAllRoles")
        .WithSummary("Get a list of all roles")
        .RequirePermission("Permissions.Roles.View")
        .WithDescription("Retrieve a list of all roles available in the system.");
    }
}
