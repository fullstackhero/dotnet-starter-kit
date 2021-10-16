using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using DN.WebApi.Shared.DTOs.Identity.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.Identity
{
    public interface IUserService : ITransientService
    {
        Task<Result<List<UserDetailsDto>>> GetAllAsync();

        Task<IResult<UserDetailsDto>> GetAsync(string userId);

        Task<IResult<UserRolesResponse>> GetRolesAsync(string userId);

        Task<IResult<string>> AssignRolesAsync(string userId, UserRolesRequest request);
        Task<Result<List<PermissionDto>>> GetPermissionsAsync(string id);
    }
}