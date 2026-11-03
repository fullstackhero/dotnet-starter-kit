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
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Authorization;

/// <summary>
/// Reconciles the permission claims of the built-in roles (<see cref="RoleConstants.Admin"/>,
/// <see cref="RoleConstants.Basic"/>) with the permission catalog for the current Finbuckle tenant
/// context: adds the missing ones and removes the ones the catalog no longer grants. Authoritative
/// because those roles are locked against manual edits, so no operator change can be lost.
/// Idempotent, so it can run on every startup safely. Removal can be turned off with
/// <see cref="PermissionSyncOptions.RemoveRevoked"/>.
/// </summary>
public sealed class RolePermissionSyncer(
    IdentityDbContext context,
    RoleManager<FshRole> roleManager,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    HybridCache cache,
    TimeProvider timeProvider,
    IOptions<PermissionSyncOptions> options,
    ILogger<RolePermissionSyncer> logger)
{
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantAccessor.MultiTenantContext.TenantInfo?.Id;
        bool isRoot = tenantId == MultitenancyConstants.Root.Id;

        int basicChanged = await SyncRoleAsync(RoleConstants.Basic, PermissionConstants.Basic, cancellationToken).ConfigureAwait(false);

        // Admin gets all non-root permissions; the root tenant's Admin additionally gets Root permissions.
        var adminPermissions = isRoot
            ? PermissionConstants.Admin.Concat(PermissionConstants.Root).Distinct().ToList()
            : PermissionConstants.Admin.ToList();
        int adminChanged = await SyncRoleAsync(RoleConstants.Admin, adminPermissions, cancellationToken).ConfigureAwait(false);

        // If we wrote anything, drop the per-user permission cache so already-logged-in
        // sessions see the change on their next request rather than waiting for TTL.
        if (basicChanged + adminChanged > 0)
        {
            await cache.RemoveByTagAsync(CacheKeys.Tags.Permissions, cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<int> SyncRoleAsync(string roleName, IReadOnlyList<FshPermission> targetPermissions, CancellationToken cancellationToken)
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingSet = existing.Select(rc => rc.ClaimValue).ToHashSet(StringComparer.Ordinal);
        var targetSet = targetPermissions.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

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

        var toRemove = SelectRevokedClaims(roleName, existing, targetSet);

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            return 0;
        }

        context.RoleClaims.RemoveRange(toRemove);
        await context.RoleClaims.AddRangeAsync(toAdd, cancellationToken).ConfigureAwait(false);
        if (!await TrySaveAsync(cancellationToken).ConfigureAwait(false))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Permission sync for '{Role}' in tenant '{Tenant}' lost a race with another instance; its changes are already applied",
                    roleName,
                    tenantAccessor.MultiTenantContext.TenantInfo?.Id);
            }

            return 0;
        }

        if (toRemove.Count > 0 && logger.IsEnabled(LogLevel.Warning))
        {
            foreach (var claim in toRemove)
            {
                logger.LogWarning(
                    "Removed permission claim '{Permission}' from '{Role}' for tenant '{Tenant}': the permission catalog no longer grants it",
                    claim.ClaimValue,
                    roleName,
                    tenantAccessor.MultiTenantContext.TenantInfo?.Id);
            }
        }

        if (toAdd.Count > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Synced {Count} new permission claim(s) to '{Role}' for tenant '{Tenant}'",
                toAdd.Count,
                roleName,
                tenantAccessor.MultiTenantContext.TenantInfo?.Id);
        }

        return toAdd.Count + toRemove.Count;
    }

    /// <summary>
    /// Saves the pending claim changes. Returns <c>false</c> when another instance starting at the same
    /// time deleted the same rows first (a delete matched 0 rows): its write already reached the same
    /// end state, so this is not an error.
    /// </summary>
    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Drop the failed entries, or the next role's save in this scope would retry them.
            context.ChangeTracker.Clear();
            return false;
        }
    }

    private List<FshRoleClaim> SelectRevokedClaims(string roleName, List<FshRoleClaim> existing, HashSet<string> targetSet)
    {
        if (!options.Value.RemoveRevoked)
        {
            return [];
        }

        // An empty target means the catalog is missing, not that the role should hold nothing.
        if (targetSet.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Skipped permission removal for '{Role}' in tenant '{Tenant}': the permission catalog grants it nothing",
                    roleName,
                    tenantAccessor.MultiTenantContext.TenantInfo?.Id);
            }

            return [];
        }

        // A null value grants nothing, so it is as stale as a retired permission.
        return existing
            .Where(rc => rc.ClaimValue is null || !targetSet.Contains(rc.ClaimValue))
            .ToList();
    }
}
