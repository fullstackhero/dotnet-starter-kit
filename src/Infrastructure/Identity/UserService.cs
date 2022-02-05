using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Common.Models;
using FSH.WebApi.Application.Common.Specification;
using FSH.WebApi.Application.Identity.Roles;
using FSH.WebApi.Application.Identity.Users;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Shared.Authorization;
using FSH.WebApi.Shared.Multitenancy;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FSH.WebApi.Infrastructure.Identity;

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

    public async Task<PaginationResponse<UserDetailsDto>> SearchAsync(UserListFilter filter, CancellationToken cancellationToken)
    {
        var spec = new EntitiesByPaginationFilterSpec<ApplicationUser>(filter);

        var users = await _userManager.Users
            .WithSpecification(spec)
            .ProjectToType<UserDetailsDto>()
            .ToListAsync(cancellationToken);
        int count = await _userManager.Users
            .CountAsync(cancellationToken);

        return new PaginationResponse<UserDetailsDto>(users, count, filter.PageNumber, filter.PageSize);
    }

    public async Task<bool> ExistsWithNameAsync(string name) =>
        await _userManager.FindByNameAsync(name) is not null;

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null) =>
        await _userManager.FindByEmailAsync(email.Normalize()) is ApplicationUser user && user.Id != exceptId;

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null) =>
        await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber) is ApplicationUser user && user.Id != exceptId;

    public async Task<List<UserDetailsDto>> GetAllAsync(CancellationToken cancellationToken) =>
        (await _userManager.Users
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .Adapt<List<UserDetailsDto>>();

    public async Task<UserDetailsDto> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException(_localizer["User Not Found."]);

        return user.Adapt<UserDetailsDto>();
    }

    public async Task<List<UserRoleDto>> GetRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var userRoles = new List<UserRoleDto>();

        var user = await _userManager.FindByIdAsync(userId);
        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var role in roles)
        {
            userRoles.Add(new UserRoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description,
                Enabled = await _userManager.IsInRoleAsync(user, role.Name)
            });
        }

        return userRoles;
    }

    public async Task<string> AssignRolesAsync(string userId, UserRolesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var user = await _userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException(_localizer["User Not Found."]);

        // Criteria : Check if Current Tenant has atleast 2 Admins and current user is not Root Tenant Admin

        // Check is there is a Disable flag for Admin in the request

        var disabledAdminRoleInRequest = request.UserRoles.Find(a => !a.Enabled && a.RoleName == FSHRoles.Admin);
        if (disabledAdminRoleInRequest is not null)
        {
            // Get count of users in Admin Role
            var adminRole = await _roleManager.Roles.Where(r => r.Name == FSHRoles.Admin).FirstOrDefaultAsync(cancellationToken);
            if (adminRole is not null)
            {
                // Guarantees Admin Role is available and the request has Disable flag for Admin Role

                // Now get count of Users with Admin Role

                int adminCount = await _context.UserRoles.Where(a => a.RoleId == adminRole.Id).CountAsync(cancellationToken);

                // Check if user is not Root Tenant Admin

                // Edge Case : there are chances for other tenants to have users with the same email as that of Root Tenant Admin. Probably can add a check while User Registration

                bool hasRootEmailAddress = user.Email == MultitenancyConstants.Root.EmailAddress;

                if (hasRootEmailAddress)
                {
                    string? tenantOfUser = await _context.Users.Where(a => a.Id == userId).Select(x => EF.Property<string>(x, "TenantId")).FirstOrDefaultAsync(cancellationToken: cancellationToken);
                    if (!string.IsNullOrEmpty(tenantOfUser) && tenantOfUser == MultitenancyConstants.Root.Id)
                    {
                        throw new ConflictException(_localizer["Cannot Remove Admin Role From Root Tenant Admin."]);
                    }
                }
                else if (adminCount <= 2)
                {
                    throw new ConflictException(_localizer["Tenant should have atleast 2 Admins."]);
                }
            }
        }

        foreach (var userRole in request.UserRoles)
        {
            // Check if Role Exists
            if (await _roleManager.FindByNameAsync(userRole.RoleName) is not null)
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

        return _localizer["User Roles Updated Successfully."];
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException(_localizer["User Not Found."]);

        var permissions = new List<PermissionDto>();
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in await _roleManager.Roles
            .Where(r => userRoles.Contains(r.Name))
            .ToListAsync(cancellationToken))
        {
            var roleClaims = await _context.RoleClaims
                .Where(rc => rc.RoleId == role.Id && rc.ClaimType == FSHClaims.Permission)
                .ToListAsync(cancellationToken);
            permissions.AddRange(roleClaims.Adapt<List<PermissionDto>>());
        }

        return permissions.Distinct().ToList();
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        _userManager.Users.AsNoTracking().CountAsync(cancellationToken);

    public async Task ToggleUserStatusAsync(ToggleUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.Where(u => u.Id == request.UserId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException(_localizer["User Not Found."]);

        bool isAdmin = await _userManager.IsInRoleAsync(user, FSHRoles.Admin);
        if (isAdmin)
        {
            throw new ConflictException(_localizer["Administrators Profile's Status cannot be toggled"]);
        }

        user.IsActive = request.ActivateUser;
        var identityResult = await _userManager.UpdateAsync(user);
    }
}