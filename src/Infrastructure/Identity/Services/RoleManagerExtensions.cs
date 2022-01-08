using System.Security.Claims;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DN.WebApi.Infrastructure.Identity.Extensions;

public static class RoleManagerExtensions
{
    public static async Task<IdentityResult> AddPermissionClaimAsync(this RoleManager<ApplicationRole> roleManager, ApplicationRole role, string permission)
    {
        var allClaims = await roleManager.GetClaimsAsync(role);
        if (!allClaims.Any(a => a.Type == FSHClaims.Permission && a.Value == permission))
        {
            return await roleManager.AddClaimAsync(role, new Claim(FSHClaims.Permission, permission));
        }

        return IdentityResult.Failed();
    }
}