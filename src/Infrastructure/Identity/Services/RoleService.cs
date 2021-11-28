using DN.WebApi.Application.Identity.Exceptions;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Extensions;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Infrastructure.Utilities;
using DN.WebApi.Shared.DTOs.Identity;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Infrastructure.Identity.Services;

public class RoleService : IRoleService
{
    private readonly ICurrentUser _currentUser;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<RoleService> _localizer;
    private readonly IRoleClaimsService _roleClaimService;

    public RoleService(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IStringLocalizer<RoleService> localizer, ICurrentUser currentUser, IRoleClaimsService roleClaimService)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
        _localizer = localizer;
        _currentUser = currentUser;
        _roleClaimService = roleClaimService;
    }

    public async Task<Result<string>> DeleteAsync(string id)
    {
        var existingRole = await _roleManager.FindByIdAsync(id);
        if (existingRole == null)
        {
            throw new IdentityException("Role Not Found", statusCode: System.Net.HttpStatusCode.NotFound);
        }

        if (DefaultRoles().Contains(existingRole.Name))
        {
            return await Result<string>.FailAsync(string.Format(_localizer["Not allowed to delete {0} Role."], existingRole.Name));
        }

        bool roleIsNotUsed = true;
        var allUsers = await _userManager.Users.ToListAsync();
        foreach (var user in allUsers)
        {
            if (await _userManager.IsInRoleAsync(user, existingRole.Name))
            {
                roleIsNotUsed = false;
            }
        }

        if (roleIsNotUsed)
        {
            await _roleManager.DeleteAsync(existingRole);
            return await Result<string>.SuccessAsync(existingRole.Id, string.Format(_localizer["Role {0} Deleted."], existingRole.Name));
        }
        else
        {
            return await Result<string>.FailAsync(string.Format(_localizer["Not allowed to delete {0} Role as it is being used."], existingRole.Name));
        }
    }

    public async Task<Result<RoleDto>> GetByIdAsync(string id)
    {
        var roles = await _roleManager.Roles.SingleOrDefaultAsync(x => x.Id == id);
        var rolesResponse = roles.Adapt<RoleDto>();
        return await Result<RoleDto>.SuccessAsync(rolesResponse);
    }

    public async Task<int> GetCountAsync()
    {
        return await _roleManager.Roles.CountAsync();
    }

    public async Task<Result<List<RoleDto>>> GetListAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var rolesResponse = roles.Adapt<List<RoleDto>>();
        return await Result<List<RoleDto>>.SuccessAsync(rolesResponse);
    }

    public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(string id)
    {
        var permissions = await _context.RoleClaims.Where(a => a.RoleId == id && a.ClaimType == "Permission").ToListAsync();
        var permissionResponse = permissions.Adapt<List<PermissionDto>>();
        return await Result<List<PermissionDto>>.SuccessAsync(permissionResponse);
    }

    public async Task<Result<List<RoleDto>>> GetUserRolesAsync(string userId)
    {
        var userRoles = await _context.UserRoles.Where(a => a.UserId == userId).Select(a => a.RoleId).ToListAsync();
        var roles = await _roleManager.Roles.Where(a => userRoles.Contains(a.Id)).ToListAsync();

        var rolesResponse = roles.Adapt<List<RoleDto>>();
        return await Result<List<RoleDto>>.SuccessAsync(rolesResponse);
    }

    public async Task<Result<string>> RegisterRoleAsync(RoleRequest request)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            var existingRole = await _roleManager.FindByNameAsync(request.Name);
            if (existingRole != null)
            {
                throw new IdentityException(_localizer["Similar Role already exists."], statusCode: System.Net.HttpStatusCode.BadRequest);
            }

            var newRole = new ApplicationRole(request.Name, _context.Tenant, request.Description);
            var response = await _roleManager.CreateAsync(newRole);
            await _context.SaveChangesAsync();
            if (response.Succeeded)
            {
                return await Result<string>.SuccessAsync(newRole.Id, string.Format(_localizer["Role {0} Created."], request.Name));
            }
            else
            {
                return await Result<string>.FailAsync(response.Errors.Select(e => _localizer[e.Description].ToString()).ToList());
            }
        }
        else
        {
            var existingRole = await _roleManager.FindByIdAsync(request.Id);
            if (existingRole == null)
            {
                return await Result<string>.FailAsync(_localizer["Role does not exist."]);
            }

            if (DefaultRoles().Contains(existingRole.Name))
            {
                return await Result<string>.SuccessAsync(string.Format(_localizer["Not allowed to modify {0} Role."], existingRole.Name));
            }

            existingRole.Name = request.Name;
            existingRole.NormalizedName = request.Name.ToUpper();
            existingRole.Description = request.Description;
            await _roleManager.UpdateAsync(existingRole);
            return await Result<string>.SuccessAsync(existingRole.Id, string.Format(_localizer["Role {0} Updated."], existingRole.Name));
        }
    }

    public async Task<Result<string>> UpdatePermissionsAsync(string roleId, List<UpdatePermissionsRequest> request)
    {
        try
        {
            var errors = new List<string>();
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return await Result<string>.FailAsync(_localizer["Role does not exist."]);
            }

            if (role.Name == RoleConstants.Admin)
            {
                var currentUser = await _userManager.Users.SingleAsync(x => x.Id == _currentUser.GetUserId().ToString());
                if (await _userManager.IsInRoleAsync(currentUser, RoleConstants.Admin))
                {
                    return await Result<string>.FailAsync(_localizer["Not allowed to modify Permissions for this Role."]);
                }
            }

            var selectedPermissions = request.Where(a => a.Enabled).ToList();
            if (role.Name == RoleConstants.Admin)
            {
                if (!selectedPermissions.Any(x => x.Permission == PermissionConstants.Roles.View)
                   || !selectedPermissions.Any(x => x.Permission == PermissionConstants.RoleClaims.View)
                   || !selectedPermissions.Any(x => x.Permission == PermissionConstants.RoleClaims.Edit))
                {
                    return await Result<string>.FailAsync(string.Format(
                        _localizer["Not allowed to deselect {0} or {1} or {2} for this Role."],
                        PermissionConstants.Roles.View,
                        PermissionConstants.RoleClaims.View,
                        PermissionConstants.RoleClaims.Edit));
                }
            }

            var permissions = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in permissions.Where(p => request.Any(a => a.Permission == p.Value)))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var permission in selectedPermissions)
            {
                var addResult = await _roleManager.AddPermissionClaimAsync(role, permission.Permission);
                if (!addResult.Succeeded)
                {
                    errors.AddRange(addResult.Errors.Select(e => _localizer[e.Description].ToString()));
                }
            }

            var addedPermissions = await _roleClaimService.GetAllByRoleIdAsync(role.Id);
            if (addedPermissions.Succeeded)
            {
                foreach (var permission in selectedPermissions)
                {
                    var addedPermission = addedPermissions.Data.SingleOrDefault(x => x.Type == ClaimConstants.Permission && x.Value == permission.Permission);
                    if (addedPermission != null)
                    {
                        var newPermission = addedPermission.Adapt<RoleClaimRequest>();
                        var saveResult = await _roleClaimService.SaveAsync(newPermission);
                        if (!saveResult.Succeeded)
                        {
                            errors.AddRange(saveResult.Messages);
                        }
                    }
                }
            }
            else
            {
                errors.AddRange(addedPermissions.Messages);
            }

            if (errors.Count > 0)
            {
                return await Result<string>.FailAsync(errors);
            }

            return await Result<string>.SuccessAsync(_localizer["Permissions Updated."]);
        }
        catch (Exception ex)
        {
            return await Result<string>.FailAsync(ex.Message);
        }
    }

    private static List<string> DefaultRoles()
    {
        return typeof(RoleConstants).GetAllPublicConstantValues<string>();
    }
}