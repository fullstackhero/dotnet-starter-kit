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

public class RoleService(RoleManager<FshRole> roleManager,
    IdentityDbContext context,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ICurrentUser currentUser) : IRoleService
{
    public async Task<IEnumerable<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        if (roleManager is null)
            throw new NotFoundException("RoleManager<FshRole> not resolved. Check Identity registration.");

        if (roleManager.Roles is null)
            throw new NotFoundException("Role store not configured. Ensure .AddRoles<FshRole>() and EF stores.");


        var roles = await roleManager.Roles
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
        FshRole? role = await roleManager.FindByIdAsync(roleId);

        if (role != null)
        {
            role.Name = name;
            role.Description = description;
            await roleManager.UpdateAsync(role);
        }
        else
        {
            role = new FshRole(name, description);
            await roleManager.CreateAsync(role);
        }

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    public async Task DeleteRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        FshRole? role = await roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("role not found");

        await roleManager.DeleteAsync(role);
    }

    public async Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await GetRoleAsync(id, cancellationToken);
        _ = role ?? throw new NotFoundException("role not found");

        role.Permissions = await context.RoleClaims
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

        ValidateRoleCanBeModified(role);
        FilterRootPermissions(permissions);

        var currentClaims = await roleManager.GetClaimsAsync(role);
        await RemoveRevokedPermissionsAsync(role, currentClaims, permissions, cancellationToken);
        await AddNewPermissionsAsync(role, currentClaims, permissions, cancellationToken);

        return "permissions updated";
    }

    private static void ValidateRoleCanBeModified(FshRole role)
    {
        if (role.Name == RoleConstants.Admin)
        {
            throw new CustomException("operation not permitted");
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
