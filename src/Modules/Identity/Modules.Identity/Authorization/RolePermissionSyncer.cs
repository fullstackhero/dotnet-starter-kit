using FSH.Framework.Caching;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Authorization;

/// <summary>
/// Adds missing permission claims to the built-in roles (<see cref="RoleConstants.Admin"/>,
/// <see cref="RoleConstants.Basic"/>) for the current Finbuckle tenant context. Idempotent —
/// only inserts claims that don't already exist, so it can run on every startup safely.
/// </summary>
public sealed class RolePermissionSyncer(
    IdentityDbContext context,
    RoleManager<FshRole> roleManager,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<RolePermissionSyncer> logger)
{
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantAccessor.MultiTenantContext.TenantInfo?.Id;
        bool isRoot = tenantId == MultitenancyConstants.Root.Id;

        int basicAdded = await SyncRoleAsync(RoleConstants.Basic, PermissionConstants.Basic, cancellationToken).ConfigureAwait(false);

        // Admin gets all non-root permissions; the root tenant's Admin additionally gets Root permissions.
        var adminPermissions = isRoot
            ? PermissionConstants.Admin.Concat(PermissionConstants.Root).Distinct().ToList()
            : PermissionConstants.Admin.ToList();
        int adminAdded = await SyncRoleAsync(RoleConstants.Admin, adminPermissions, cancellationToken).ConfigureAwait(false);

        // If we wrote anything, drop the per-user permission cache so already-logged-in
        // sessions see the new perms on their next request rather than waiting for TTL.
        if (basicAdded + adminAdded > 0)
        {
            await cache.RemoveByTagAsync(CacheKeys.Tags.Permissions, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> SyncRoleAsync(string roleName, IReadOnlyList<FshPermission> targetPermissions, CancellationToken cancellationToken)
    {
        var role = await roleManager.Roles
            .SingleOrDefaultAsync(r => r.Name == roleName, cancellationToken)
            .ConfigureAwait(false);
        if (role is null)
        {
            // Role not yet seeded — full IdentityDbInitializer.SeedAsync will create it the first time.
            return 0;
        }

        var existing = await context.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == ClaimConstants.Permission)
            .Select(rc => rc.ClaimValue!)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);

        var toAdd = targetPermissions
            .Where(p => !existingSet.Contains(p.Name))
            .Select(p => new FshRoleClaim
            {
                RoleId = role.Id,
                ClaimType = ClaimConstants.Permission,
                ClaimValue = p.Name,
                CreatedBy = "RolePermissionSyncer",
                CreatedOn = timeProvider.GetUtcNow(),
            })
            .ToList();

        if (toAdd.Count == 0)
        {
            return 0;
        }

        await context.RoleClaims.AddRangeAsync(toAdd, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Synced {Count} new permission claim(s) to '{Role}' for tenant '{Tenant}'",
                toAdd.Count,
                roleName,
                tenantAccessor.MultiTenantContext.TenantInfo?.Id);
        }

        return toAdd.Count;
    }
}
