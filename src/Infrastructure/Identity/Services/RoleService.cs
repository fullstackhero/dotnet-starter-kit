using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Extensions;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Infrastructure.Utilties;
using DN.WebApi.Shared.DTOs.Identity;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using DN.WebApi.Shared.DTOs.Identity.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;

namespace DN.WebApi.Infrastructure.Identity.Services
{
    public class RoleService : IRoleService
    {
        private readonly ICurrentUser _currentUser;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<RoleService> _localizer;

        public RoleService(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IStringLocalizer<RoleService> localizer, ICurrentUser currentUser)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
            _localizer = localizer;
            _currentUser = currentUser;
        }

        private static List<string> DefaultRoles()
        {
            return typeof(RoleConstants).GetAllPublicConstantValues<string>();
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

        public Task<Result<List<RoleClaimResponse>>> GetAllPermissionsAsync()
        {
            throw new System.NotImplementedException();
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

        public Task<Result<PermissionResponse>> GetRolePermissionsAsync(string id)
        {
            return default;
        }

        public async Task<Result<List<RoleDto>>> GetUserRolesAsync(string userId)
        {
            var userRoles = await _context.UserRoles.Where(a => a.UserId == userId).Select(a => a.RoleId).ToListAsync();
            var roles = await _roleManager.Roles.Where(a => userRoles.Contains(a.Id)).ToListAsync();
            var rolesResponse = roles.Adapt<List<RoleDto>>();
            return await Result<List<RoleDto>>.SuccessAsync(rolesResponse);
        }

        public async Task<Result<string>> SaveAsync(RoleRequest request)
        {
            if (string.IsNullOrEmpty(request.Id))
            {
                var existingRole = await _roleManager.FindByNameAsync(request.Name);
                if (existingRole != null)
                {
                    throw new IdentityException(_localizer["Similar Role already exists."], statusCode: System.Net.HttpStatusCode.BadRequest);
                }

                var newRole = new ApplicationRole(request.Name, _context.TenantKey, request.Description);
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
    }
}