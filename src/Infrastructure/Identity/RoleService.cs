using System.Security.Claims;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Application.Identity;
using FSH.WebApi.Application.Identity.Roles;
using FSH.WebApi.Infrastructure.Common.Extensions;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Shared.Authorization;
using FSH.WebApi.Shared.Multitenancy;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FSH.WebApi.Infrastructure.Identity;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<RoleService> _localizer;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IStringLocalizer<RoleService> localizer)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
        _localizer = localizer;
    }

    public async Task<List<RoleDto>> GetListAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();

        var roleDtos = roles.Adapt<List<RoleDto>>();
        roleDtos.ForEach(role => role.IsDefault = DefaultRoles.Contains(role.Name));

        return roleDtos;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        await _roleManager.Roles.CountAsync(cancellationToken);

    public async Task<bool> ExistsAsync(string roleName, string? excludeId) =>
        await _roleManager.FindByNameAsync(roleName)
            is ApplicationRole existingRole
            && existingRole.Id != excludeId;

    public async Task<RoleDto> GetByIdAsync(string id)
    {
        var role = await _context.Roles.SingleOrDefaultAsync(x => x.Id == id);
        _ = role ?? throw new NotFoundException(_localizer["Role Not Found"]);
        var roleDto = role.Adapt<RoleDto>();
        roleDto.IsDefault = DefaultRoles.Contains(role.Name);
        return roleDto;
    }

    /// <summary>
    /// Get Permissions By Role Async.
    /// </summary>
    public async Task<RoleDto> GetByIdWithPermissionsAsync(string roleId, CancellationToken cancellationToken)
    {
        var role = await GetByIdAsync(roleId);

        role.Permissions = (await _context.RoleClaims
            .Where(a => a.RoleId == roleId && a.ClaimType == FSHClaims.Permission)
            .ToListAsync(cancellationToken))
            .Adapt<List<PermissionDto>>();

        string? tenantOfRole = await _context.Roles.Where(a => a.Id == roleId).Select(x => EF.Property<string>(x, "TenantId")).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (tenantOfRole == MultitenancyConstants.Root.Id) role.IsRootRole = true;

        return role;
    }

    public async Task<string> RegisterRoleAsync(RoleRequest request)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            var newRole = new ApplicationRole(request.Name, request.Description);
            var result = await _roleManager.CreateAsync(newRole);

            return result.Succeeded
                ? string.Format(_localizer["Role {0} Created."], request.Name)
                : throw new InternalServerException(_localizer["Register role failed"], result.Errors.Select(e => _localizer[e.Description].ToString()).ToList());
        }
        else
        {
            var role = await _roleManager.FindByIdAsync(request.Id);

            _ = role ?? throw new NotFoundException(_localizer["Role Not Found"]);

            if (DefaultRoles.Contains(role.Name))
            {
                throw new ConflictException(string.Format(_localizer["Not allowed to modify {0} Role."], role.Name));
            }

            role.Name = request.Name;
            role.NormalizedName = request.Name.ToUpperInvariant();
            role.Description = request.Description;
            var result = await _roleManager.UpdateAsync(role);

            return result.Succeeded
                ? string.Format(_localizer["Role {0} Updated."], role.Name)
                : throw new InternalServerException(_localizer["Update role failed"], result.Errors.Select(e => _localizer[e.Description].ToString()).ToList());
        }
    }

    /// <summary>
    /// Update Permissions by Role Async.
    /// </summary>
    public async Task<string> UpdatePermissionsAsync(UpdatePermissionsRequest request, CancellationToken cancellationToken)
    {
        var selectedPermissions = request.Permissions;
        var role = await _roleManager.FindByIdAsync(request.RoleId);
        _ = role ?? throw new NotFoundException(_localizer["Role Not Found"]);
        if (role.Name == FSHRoles.Admin)
        {
            throw new ConflictException(_localizer["Not allowed to modify Permissions for this Role."]);
        }

        string? tenantOfRole = await _context.Roles.Where(a => a.Id == request.RoleId).Select(x => EF.Property<string>(x, "TenantId")).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (tenantOfRole != MultitenancyConstants.Root.Id)
        {
            // Remove Root Permissions if the Role is not created for Root Tenant.
            request.Permissions.RemoveAll(u => u.StartsWith("Permissions.Root."));
        }

        var currentPermissions = await _roleManager.GetClaimsAsync(role);

        // Remove permissions that were previously selected
        foreach (var claim in currentPermissions.Where(c => !selectedPermissions.Any(p => p == c.Value)))
        {
            var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removeResult.Succeeded)
            {
                throw new InternalServerException(_localizer["Update permissions failed."], removeResult.Errors.Select(e => _localizer[e.Description].ToString()).ToList());
            }
        }

        // Add all permissions that were not previously selected
        foreach (string permission in selectedPermissions.Where(c => !currentPermissions.Any(p => p.Value == c)))
        {
            if (!string.IsNullOrEmpty(permission))
            {
                var addResult = await _roleManager.AddClaimAsync(role, new Claim(FSHClaims.Permission, permission));
                if (!addResult.Succeeded)
                {
                    throw new InternalServerException(_localizer["Update permissions failed."], addResult.Errors.Select(e => _localizer[e.Description].ToString()).ToList());
                }
            }
        }

        return _localizer["Permissions Updated."];
    }

    public async Task<string> DeleteAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException(_localizer["Role Not Found"]);

        if (DefaultRoles.Contains(role.Name))
        {
            throw new ConflictException(string.Format(_localizer["Not allowed to delete {0} Role."], role.Name));
        }

        bool roleIsNotUsed = true;
        var allUsers = await _userManager.Users.ToListAsync();
        foreach (var user in allUsers)
        {
            if (await _userManager.IsInRoleAsync(user, role.Name))
            {
                roleIsNotUsed = false;
            }
        }

        if (roleIsNotUsed)
        {
            await _roleManager.DeleteAsync(role);
            return string.Format(_localizer["Role {0} Deleted."], role.Name);
        }
        else
        {
            throw new ConflictException(string.Format(_localizer["Not allowed to delete {0} Role as it is being used."], role.Name));
        }
    }

    internal static List<string> DefaultRoles =>
        typeof(FSHRoles).GetAllPublicConstantValues<string>();
}