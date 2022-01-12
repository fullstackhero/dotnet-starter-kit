using System.Security.Claims;
using FSH.WebApi.Application.Identity.RoleClaims;
using Microsoft.AspNetCore.Authorization;

namespace FSH.WebApi.Infrastructure.Auth.Permissions;

internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRoleClaimsService _permissionService;

    public PermissionAuthorizationHandler(IRoleClaimsService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        string? userId = context.User?.GetUserId();
        if (userId is not null &&
            await _permissionService.HasPermissionAsync(userId, requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}