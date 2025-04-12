using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.ExecutionContext;
using FSH.Framework.Identity.Core.Roles;
using FSH.Framework.Identity.Endpoints.v1.Roles.CreateOrUpdateRole;
using FSH.Framework.Identity.Endpoints.v1.Roles.UpdatePermissions;
using FSH.Framework.Identity.Infrastructure.Data;
using FSH.Framework.Identity.Infrastructure.Roles;
using FSH.Framework.Infrastructure.Identity.RoleClaims;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Infrastructure.Identity.Roles;

public class RoleService(RoleManager<FshRole> roleManager,
    IdentityDbContext context,
    IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor,
    ICurrentUser currentUser) : IRoleService
{
    private readonly RoleManager<FshRole> _roleManager = roleManager;

    public async Task<IEnumerable<RoleDto>> GetRolesAsync()
    {
        return await Task.Run(() => _roleManager.Roles
            .Select(role => new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description })
            .ToList());
    }

    public async Task<RoleDto?> GetRoleAsync(string id)
    {
        FshRole? role = await _roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("role not found");

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    public async Task<RoleDto> CreateOrUpdateRoleAsync(UpsertRoleCommand command)
    {
        FshRole? role = await _roleManager.FindByIdAsync(command.Id);

        if (role != null)
        {
            role.Name = command.Name;
            role.Description = command.Description;
            await _roleManager.UpdateAsync(role);
        }
        else
        {
            role = new FshRole(command.Name, command.Description);
            await _roleManager.CreateAsync(role);
        }

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    public async Task DeleteRoleAsync(string id)
    {
        FshRole? role = await _roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("role not found");

        await _roleManager.DeleteAsync(role);
    }

    public async Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken)
    {
        var role = await GetRoleAsync(id);
        _ = role ?? throw new NotFoundException("role not found");

        role.Permissions = await context.RoleClaims
            .Where(c => c.RoleId == id && c.ClaimType == FshClaims.Permission)
            .Select(c => c.ClaimValue!)
            .ToListAsync(cancellationToken);

        return role;
    }

    public async Task<string> UpdatePermissionsAsync(UpdatePermissionsCommand command)
    {
        var role = await _roleManager.FindByIdAsync(command.RoleId);
        _ = role ?? throw new NotFoundException("role not found");
        if (role.Name == FshRoles.Admin)
        {
            throw new CustomException("operation not permitted");
        }

        if (multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id != TenantConstants.Root.Id)
        {
            // Remove Root Permissions if the Role is not created for Root Tenant.
            command.Permissions.RemoveAll(u => u.StartsWith("Permissions.Root.", StringComparison.InvariantCultureIgnoreCase));
        }

        var currentClaims = await _roleManager.GetClaimsAsync(role);

        // Remove permissions that were previously selected
        foreach (var claim in currentClaims.Where(c => !command.Permissions.Exists(p => p == c.Value)))
        {
            var result = await _roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description).ToList();
                throw new CustomException("operation failed", errors);
            }
        }

        // Add all permissions that were not previously selected
        foreach (string permission in command.Permissions.Where(c => !currentClaims.Any(p => p.Value == c)))
        {
            if (!string.IsNullOrEmpty(permission))
            {
                context.RoleClaims.Add(new FshRoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = FshClaims.Permission,
                    ClaimValue = permission,
                    CreatedBy = currentUser.GetUserId().ToString()
                });
                await context.SaveChangesAsync();
            }
        }

        return "permissions updated";
    }
}
