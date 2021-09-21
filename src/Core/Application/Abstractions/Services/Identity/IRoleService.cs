using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using DN.WebApi.Shared.DTOs.Identity.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.Identity
{
    public interface IRoleService : ITransientService
    {
        Task<Result<List<RoleDto>>> GetListAsync();

        Task<int> GetCountAsync();

        Task<Result<RoleDto>> GetByIdAsync(string id);

        Task<Result<string>> SaveAsync(RoleRequest request);

        Task<Result<string>> DeleteAsync(string id);

        Task<Result<List<RoleDto>>> GetUserRolesAsync(string userId);

        Task<Result<PermissionResponse>> GetRolePermissionsAsync(string id);

        Task<Result<List<RoleClaimResponse>>> GetAllPermissionsAsync();
    }
}