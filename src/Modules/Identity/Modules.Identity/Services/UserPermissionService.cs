using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.Identity.Services;

internal sealed class UserPermissionService(
    UserManager<FshUser> userManager,
    RoleManager<FshRole> roleManager,
    IdentityDbContext db,
    HybridCache cache) : IUserPermissionService
{
    public async Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    {
        var tags = new[] { CacheKeys.Tags.Permissions, CacheKeys.Tags.User(userId) };

        var permissions = await cache.GetOrCreateAsync(
            CacheKeys.UserPermissions(userId),
            async ct =>
            {
                var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
                _ = user ?? throw new UnauthorizedException();

                var userRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
                var result = new List<string>();
                foreach (var role in await roleManager.Roles
                    .Where(r => userRoles.Contains(r.Name!))
                    .ToListAsync(ct).ConfigureAwait(false))
                {
                    result.AddRange(await db.RoleClaims
                        .Where(rc => rc.RoleId == role.Id && rc.ClaimType == ClaimConstants.Permission)
                        .Select(rc => rc.ClaimValue!)
                        .ToListAsync(ct).ConfigureAwait(false));
                }
                return result.Distinct().ToList();
            },
            tags: tags,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(userId, cancellationToken).ConfigureAwait(false);
        return permissions?.Contains(permission) ?? false;
    }

    public Task InvalidatePermissionCacheAsync(string userId, CancellationToken cancellationToken)
        => cache.RemoveAsync(CacheKeys.UserPermissions(userId), cancellationToken).AsTask();
}
