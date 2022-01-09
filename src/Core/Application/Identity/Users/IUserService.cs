using DN.WebApi.Application.Common;
using DN.WebApi.Application.Wrapper;

namespace DN.WebApi.Application.Identity.Users;

public interface IUserService : ITransientService
{
    Task<PaginatedResult<UserDetailsDto>> SearchAsync(UserListFilter filter);

    Task<Result<List<UserDetailsDto>>> GetAllAsync();

    Task<int> GetCountAsync();

    Task<IResult<UserDetailsDto>> GetAsync(string userId);

    Task<IResult<UserRolesResponse>> GetRolesAsync(string userId);

    Task<IResult<string>> AssignRolesAsync(string userId, UserRolesRequest request);

    Task<Result<List<PermissionDto>>> GetPermissionsAsync(string id);

    Task<IResult> ToggleUserStatusAsync(ToggleUserStatusRequest request);
}