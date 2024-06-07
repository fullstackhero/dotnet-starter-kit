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

public static class GetRoleByIdEndpoint
{
    public static RouteHandlerBuilder MapGetRoleByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/roles/{id}", async (string id, IRoleService roleService) =>
        {
            var role = await roleService.GetRoleByIdAsync(id);
            return role != null ? Results.Ok(role) : Results.NotFound();
        })
        .WithName("GetRoleById")
        .WithSummary("Get role details by ID")
        .WithDescription("Retrieve the details of a role by its ID.");
    }
}

