using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using GrpcShared.Controllers;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Identity;

public class UsersControllerGrpc : IUsersControllerGrpc
{
    private readonly IUserService _userService;

    public UsersControllerGrpc(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<Result<string>> AssignRolesAsync(UserRolesRequest request, CallContext context)
    {
        var result = await _userService.AssignRolesAsync(request.Id, request);
        return result;
    }

    public async Task<Result<List<UserDetailsDto>>> GetAllAsync(CallContext context)
    {
        var users = await _userService.GetAllAsync();
        return users;
    }

    public async Task<Result<UserDetailsDto>> GetByIdAsync(string userId, CallContext context)
    {
        var user = await _userService.GetAsync(userId);
        return user;
    }

    public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(string id, CallContext context)
    {
        var userPermissions = await _userService.GetPermissionsAsync(id);
        return userPermissions;
    }

    public async Task<Result<UserRolesResponse>> GetRolesAsync(string userId, CallContext context)
    {
        var userRoles = await _userService.GetRolesAsync(userId);
        return userRoles;
    }
}
