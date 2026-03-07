using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Services;

internal sealed class UserPermissionService(
    UserManager<FshUser> userManager,
    RoleManager<FshRole> roleManager,
    IdentityDbContext db,
    ICacheService cache,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor) : IUserPermissionService
{
    public async Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    {
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id ?? throw new InvalidOperationException("Tenant context required.");

        var permissions = await cache.GetOrSetAsync(
            GetPermissionCacheKey(tenantId, userId),
            async () =>
            {
                var user = await userManager.FindByIdAsync(userId);

                _ = user ?? throw new UnauthorizedException();

                var userRoles = await userManager.GetRolesAsync(user);
                var permissions = new List<string>();
                foreach (var role in await roleManager.Roles
                    .Where(r => userRoles.Contains(r.Name!))
                    .ToListAsync(cancellationToken))
                {
                    permissions.AddRange(await db.RoleClaims
                        .Where(rc => rc.RoleId == role.Id && rc.ClaimType == ClaimConstants.Permission)
                        .Select(rc => rc.ClaimValue!)
                        .ToListAsync(cancellationToken));
                }
                return permissions.Distinct().ToList();
            },
            cancellationToken: cancellationToken);

        return permissions;
    }

    public static string GetPermissionCacheKey(string tenantId, string userId)
    {
        return $"perm:{tenantId}:{userId}";
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(userId, cancellationToken);

        return permissions?.Contains(permission) ?? false;
    }

    public Task InvalidatePermissionCacheAsync(string userId, CancellationToken cancellationToken)
    {
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id ?? throw new InvalidOperationException("Tenant context required.");
        return cache.RemoveItemAsync(GetPermissionCacheKey(tenantId, userId), cancellationToken);
    }
}
