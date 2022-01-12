using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Application.Identity;
using FSH.WebApi.Application.Identity.RoleClaims;
using FSH.WebApi.Application.Identity.Roles;
using FSH.WebApi.Infrastructure.Common.Extensions;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Shared.Authorization;
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
    private readonly ICurrentUser _currentUser;
    private readonly IRoleClaimsService _roleClaimService;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IStringLocalizer<RoleService> localizer,
        ICurrentUser currentUser,
        IRoleClaimsService roleClaimService)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
        _localizer = localizer;
        _currentUser = currentUser;
        _roleClaimService = roleClaimService;
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

    public async Task<RoleDto> GetByIdAsync(string id)
    {
        var role = await _roleManager.Roles.SingleOrDefaultAsync(x => x.Id == id);

        _ = role ?? throw new NotFoundException(_localizer["Role Not Found"]);

        var roleDto = role.Adapt<RoleDto>();
        roleDto.IsDefault = DefaultRoles.Contains(role.Name);

        return roleDto;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        await _roleManager.Roles.CountAsync(cancellationToken);

    public async Task<List<RoleDto>> GetListAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();

        var roleDtos = roles.Adapt<List<RoleDto>>();
        roleDtos.ForEach(role => role.IsDefault = DefaultRoles.Contains(role.Name));

        return roleDtos;
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(string id, CancellationToken cancellationToken)
    {
        var permissions = await _context.RoleClaims
            .Where(a => a.RoleId == id && a.ClaimType == FSHClaims.Permission)
            .ToListAsync(cancellationToken);

        return permissions.Adapt<List<PermissionDto>>();
    }

    public async Task<List<RoleDto>> GetUserRolesAsync(string userId)
    {
        var userRoles = await _context.UserRoles.Where(a => a.UserId == userId).Select(a => a.RoleId).ToListAsync();
        var roles = await _roleManager.Roles.Where(a => userRoles.Contains(a.Id)).ToListAsync();

        var roleDtos = roles.Adapt<List<RoleDto>>();
        roleDtos.ForEach(role => role.IsDefault = DefaultRoles.Contains(role.Name));

        return roleDtos;
    }

    public async Task<bool> ExistsAsync(string roleName, string? excludeId) =>
        await _roleManager.FindByNameAsync(roleName)
            is ApplicationRole existingRole
            && existingRole.Id != excludeId;

    public async Task<string> RegisterRoleAsync(RoleRequest request)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            var newRole = new ApplicationRole(request.Name, _context.TenantKey, request.Description);
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

    public async Task<string> UpdatePermissionsAsync(string roleId, List<UpdatePermissionsRequest> permissions, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var role = await _roleManager.FindByIdAsync(roleId);
        _ = role ?? throw new NotFoundException(_localizer["Role Not Found"]);

        if (role.Name == FSHRoles.Admin)
        {
            var currentUser = await _userManager.Users.SingleAsync(x => x.Id == _currentUser.GetUserId().ToString());
            if (!await _userManager.IsInRoleAsync(currentUser, FSHRoles.Admin))
            {
                throw new ConflictException(_localizer["Not allowed to modify Permissions for this Role."]);
            }
        }

        var selectedPermissions = permissions.Where(p => p.Enabled).ToList();
        if (role.Name == FSHRoles.Admin)
        {
            if (!selectedPermissions.Any(x => x.Permission == FSHPermissions.Roles.View)
                || !selectedPermissions.Any(x => x.Permission == FSHPermissions.RoleClaims.View)
                || !selectedPermissions.Any(x => x.Permission == FSHPermissions.RoleClaims.Edit))
            {
                throw new ConflictException(string.Format(
                    _localizer["Not allowed to deselect {0} or {1} or {2} for this Role."],
                    FSHPermissions.Roles.View,
                    FSHPermissions.RoleClaims.View,
                    FSHPermissions.RoleClaims.Edit));
            }
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in claims.Where(c => permissions.Any(p => p.Permission == c.Value)))
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var permission in selectedPermissions)
        {
            if (!string.IsNullOrEmpty(permission.Permission))
            {
                var addResult = await _roleManager.AddPermissionClaimAsync(role, permission.Permission);
                if (!addResult.Succeeded)
                {
                    errors.AddRange(addResult.Errors.Select(e => _localizer[e.Description].ToString()));
                }
            }
        }

        var allPermissions = await _roleClaimService.GetAllByRoleIdAsync(role.Id, cancellationToken);
        foreach (var permission in selectedPermissions)
        {
            if (allPermissions.SingleOrDefault(x => x.Type == FSHClaims.Permission && x.Value == permission.Permission)
                is RoleClaimDto addedPermission)
            {
                await _roleClaimService.SaveAsync(addedPermission.Adapt<RoleClaimRequest>(), cancellationToken);
            }
        }

        if (errors.Count > 0)
        {
            throw new InternalServerException(_localizer["Update permissions failed."], errors);
        }

        return _localizer["Permissions Updated."];
    }

    internal static List<string> DefaultRoles =>
        typeof(FSHRoles).GetAllPublicConstantValues<string>();
}