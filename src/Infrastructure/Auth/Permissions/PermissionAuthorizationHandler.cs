using System.Security.Claims;
using FSH.WebApi.Application.Identity.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace FSH.WebApi.Infrastructure.Auth.Permissions;

internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserService _userService;
    private IConfiguration Config { get; set; }

    public PermissionAuthorizationHandler(IUserService userService, IConfiguration config) {
        _userService = userService;
        Config = config;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (Config.GetSection("FeatureFlagSettings").GetSection("Auth").Value == "True") {
            if (context.User?.GetUserId() is { } userId &&
                await _userService.HasPermissionAsync(userId, requirement.Permission)) {
                context.Succeed(requirement);
            }
        }
        else { context.Succeed(requirement); }
    }
}