using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Common.Extensions;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Infrastructure.Persistence.Converters;
using DN.WebApi.Infrastructure.Persistence.Repositories;
using DN.WebApi.Shared.DTOs.Identity;
using DN.WebApi.Application.Identity.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq.Dynamic.Core;

namespace DN.WebApi.Infrastructure.Identity.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IStringLocalizer<UserService> _localizer;

    private readonly ApplicationDbContext _context;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IStringLocalizer<UserService> localizer,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _localizer = localizer;
        _context = context;
    }

    public async Task<PaginatedResult<UserDetailsDto>> SearchAsync(UserListFilter filter)
    {
        var filters = new Filters<ApplicationUser>();
        filters.Add(filter.IsActive.HasValue, x => x.IsActive == filter.IsActive);

        var query = _userManager.Users.ApplyFilter(filters).AdvancedSearch(filter.AdvancedSearch);
        string ordering = new OrderByConverter().ConvertBack(filter.OrderBy);
        query = !string.IsNullOrWhiteSpace(ordering) ? query.OrderBy(ordering) : query.OrderBy(a => a.Id);

        return await query.ToMappedPaginatedResultAsync<ApplicationUser, UserDetailsDto>(filter.PageNumber, filter.PageSize);
    }

    public async Task<Result<List<UserDetailsDto>>> GetAllAsync()
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        var result = users.Adapt<List<UserDetailsDto>>();
        return await Result<List<UserDetailsDto>>.SuccessAsync(result);
    }

    public async Task<IResult<UserDetailsDto>> GetAsync(string userId)
    {
        var user = await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).FirstOrDefaultAsync();
        var result = user.Adapt<UserDetailsDto>();
        return await Result<UserDetailsDto>.SuccessAsync(result);
    }

    public async Task<IResult<UserRolesResponse>> GetRolesAsync(string userId)
    {
        var viewModel = new List<UserRoleDto>();
        var user = await _userManager.FindByIdAsync(userId);
        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync();
        foreach (var role in roles)
        {
            var userRolesViewModel = new UserRoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name
            };
            userRolesViewModel.Enabled = await _userManager.IsInRoleAsync(user, role.Name);

            viewModel.Add(userRolesViewModel);
        }

        var result = new UserRolesResponse { UserRoles = viewModel };
        return await Result<UserRolesResponse>.SuccessAsync(result);
    }

    public async Task<IResult<string>> AssignRolesAsync(string userId, UserRolesRequest request)
    {
        var user = await _userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
        {
            return await Result<string>.FailAsync(_localizer["User Not Found."]);
        }

        if (await _userManager.IsInRoleAsync(user, RoleConstants.Admin))
        {
            return await Result<string>.FailAsync(_localizer["Not Allowed."]);
        }

        foreach (var userRole in request.UserRoles)
        {
            // Check if Role Exists
            if (await _roleManager.FindByNameAsync(userRole.RoleName) != null)
            {
                if (userRole.Enabled)
                {
                    if (!await _userManager.IsInRoleAsync(user, userRole.RoleName))
                    {
                        await _userManager.AddToRoleAsync(user, userRole.RoleName);
                    }
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, userRole.RoleName);
                }
            }
        }

        return await Result<string>.SuccessAsync(userId, string.Format(_localizer["User Roles Updated Successfully."]));
    }

    public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(string userId)
    {
        var userPermissions = new List<PermissionDto>();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return await Result<List<PermissionDto>>.FailAsync(_localizer["User Not Found."]);
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        foreach (var role in _roleManager.Roles.Where(r => roleNames.Contains(r.Name)).ToList())
        {
            var permissions = await _context.RoleClaims.Where(a => a.RoleId == role.Id && a.ClaimType == "Permission").ToListAsync();
            var permissionResponse = permissions.Adapt<List<PermissionDto>>();
            userPermissions.AddRange(permissionResponse);
        }

        return await Result<List<PermissionDto>>.SuccessAsync(userPermissions.Distinct().ToList());
    }

    public async Task<int> GetCountAsync()
    {
        return await _userManager.Users.AsNoTracking().CountAsync();
    }
}