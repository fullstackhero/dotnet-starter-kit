using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Roles;

public sealed class RoleService(RoleManager<FshRole> roleManager,
    IdentityDbContext context,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ICurrentUser currentUser,
    IUserPermissionService userPermissionService) : IRoleService
{
    // Invalidate every user whose effective permission set may have shifted as a
    // result of a role-level mutation. "Direct" = AspNetUserRoles, "via groups"
    // = members of groups that carry this role.
    private async Task InvalidateAffectedUsersAsync(string roleId, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role?.Name is null)
        {
            return;
        }

        // Direct role holders via AspNetUserRoles join.
        var directUserIds = await context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);

        // Group-derived role holders.
        var groupUserIds = await context.GroupRoles
            .Where(gr => gr.RoleId == roleId)
            .SelectMany(gr => context.UserGroups
                .Where(ug => ug.GroupId == gr.GroupId)
                .Select(ug => ug.UserId))
            .ToListAsync(cancellationToken);

        foreach (var userId in directUserIds.Concat(groupUserIds).Distinct())
        {
            await userPermissionService.InvalidatePermissionCacheAsync(userId, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IEnumerable<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        if (roleManager is null)
            throw new NotFoundException("RoleManager<FshRole> not resolved. Check Identity registration.");

        if (roleManager.Roles is null)
            throw new NotFoundException("Role store not configured. Ensure .AddRoles<FshRole>() and EF stores.");


        var roles = await roleManager.Roles
            .AsNoTracking()
            .Select(role => new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description })
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task<RoleDto?> GetRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        FshRole? role = await roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("role not found");

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    public async Task<RoleDto> CreateOrUpdateRoleAsync(string roleId, string name, string description, CancellationToken cancellationToken = default)
    {
        FshRole? role = string.IsNullOrEmpty(roleId)
            ? null
            : await roleManager.FindByIdAsync(roleId);

        if (role != null)
        {
            // System roles cannot be modified — neither renamed nor re-described.
            EnsureNotSystemRole(role.Name, "System roles cannot be modified.");
            // And no custom role can be renamed to a system role's name.
            EnsureNotSystemRole(name, "Cannot rename a role to a system role's name.");

            role.Name = name;
            role.Description = description;
            await roleManager.UpdateAsync(role);
        }
        else
        {
            // No new role can be created using a system role's name.
            EnsureNotSystemRole(name, "Cannot create a role using a system role's name.");

            role = new FshRole(name, description);
            await roleManager.CreateAsync(role);
        }

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    public async Task DeleteRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        FshRole? role = await roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("role not found");

        EnsureNotSystemRole(role.Name, "System roles cannot be deleted.");

        // Snapshot affected users BEFORE the cascade removes the role-mapping rows,
        // otherwise the lookup returns an empty set after delete.
        await InvalidateAffectedUsersAsync(id, cancellationToken).ConfigureAwait(false);

        await roleManager.DeleteAsync(role);
    }

    public async Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await GetRoleAsync(id, cancellationToken);
        _ = role ?? throw new NotFoundException("role not found");

        role.Permissions = await context.RoleClaims
            .AsNoTracking()
            .Where(c => c.RoleId == id && c.ClaimType == ClaimConstants.Permission)
            .Select(c => c.ClaimValue!)
            .ToListAsync(cancellationToken);

        return role;
    }

    public async Task<string> UpdatePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        var role = await roleManager.FindByIdAsync(roleId)
            ?? throw new NotFoundException("role not found");

        EnsureNotSystemRole(role.Name, "System role permissions are managed by the framework and cannot be modified.");
        FilterRootPermissions(permissions);

        var currentClaims = await roleManager.GetClaimsAsync(role);
        await RemoveRevokedPermissionsAsync(role, currentClaims, permissions, cancellationToken);
        await AddNewPermissionsAsync(role, currentClaims, permissions, cancellationToken);

        // Permissions on the role just changed — every user reachable through this
        // role (directly or via group membership) now has a stale cache entry.
        await InvalidateAffectedUsersAsync(roleId, cancellationToken).ConfigureAwait(false);

        return "permissions updated";
    }

    private static void EnsureNotSystemRole(string? roleName, string message)
    {
        if (!string.IsNullOrEmpty(roleName) && RoleConstants.IsDefault(roleName))
        {
            throw new CustomException(message, Array.Empty<string>(), HttpStatusCode.BadRequest);
        }
    }

    private void FilterRootPermissions(List<string> permissions)
    {
        if (multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id != MultitenancyConstants.Root.Id)
        {
            permissions.RemoveAll(u => u.StartsWith("Permissions.Root.", StringComparison.InvariantCultureIgnoreCase));
        }
    }

    private async Task RemoveRevokedPermissionsAsync(FshRole role, IList<System.Security.Claims.Claim> currentClaims, List<string> permissions, CancellationToken cancellationToken = default)
    {
        var claimsToRemove = currentClaims.Where(c => !permissions.Exists(p => p == c.Value));

        foreach (var claim in claimsToRemove)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description).ToList();
                throw new CustomException("operation failed", errors);
            }
        }
    }

    private async Task AddNewPermissionsAsync(FshRole role, IList<System.Security.Claims.Claim> currentClaims, List<string> permissions, CancellationToken cancellationToken = default)
    {
        var newPermissions = permissions
            .Where(p => !string.IsNullOrEmpty(p) && !currentClaims.Any(c => c.Value == p))
            .ToList();

        foreach (string permission in newPermissions)
        {
            context.RoleClaims.Add(new FshRoleClaim
            {
                RoleId = role.Id,
                ClaimType = ClaimConstants.Permission,
                ClaimValue = permission,
                CreatedBy = currentUser.GetUserId().ToString()
            });
        }

        if (newPermissions.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}