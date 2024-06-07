using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSH.Framework.Core.Identity.Roles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Roles.Endpoints;
public static class CreateOrUpdateRoleEndpoint
{
    public static RouteHandlerBuilder MapCreateOrUpdateRoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/roles", async (FshRole role, IRoleService roleService) =>
        {
            var result = await roleService.CreateOrUpdateRoleAsync(role);
            return Results.Ok(result);
        })
        .WithName("CreateOrUpdateRole")
        .WithSummary("Create or update a role")
        .WithDescription("Create a new role or update an existing role.");
    }
}
