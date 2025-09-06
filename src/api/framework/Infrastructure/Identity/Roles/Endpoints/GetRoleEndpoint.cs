using FSH.Framework.Core.Identity.Roles;
using FSH.Framework.Infrastructure.Auth.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Roles.Endpoints;

public static class GetRoleByIdEndpoint
{
    public static RouteHandlerBuilder MapGetRoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id:guid}", async (string id, IRoleService roleService) =>
        {
            return await roleService.GetRoleAsync(id);
        })
        .WithName(nameof(GetRoleByIdEndpoint))
        .WithSummary("Get role details by ID")
        .RequirePermission("Permissions.Roles.View")
        .WithDescription("Retrieve the details of a role by its ID.");
    }
}

