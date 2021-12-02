using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Identity;
using GrpcShared.Controllers;
using GrpcShared.Models;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Identity;

public class RolesControllerGrpc : IRolesControllerGrpc
{
    private readonly IRoleService _roleService;

    public RolesControllerGrpc(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [MustHavePermission(PermissionConstants.Roles.ListAll)]
    public async Task<Result<List<RoleDto>>> GetListAsync(CallContext context = default)
    {
        var roles = await _roleService.GetListAsync();
        return roles;
    }

    [MustHavePermission(PermissionConstants.Roles.View)]
    public async Task<Result<RoleDto>> GetByIdAsync(string id, CallContext context = default)
    {
        var roles = await _roleService.GetByIdAsync(id);
        return roles;
    }

    public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(string id, CallContext context = default)
    {
        var roles = await _roleService.GetPermissionsAsync(id);
        return roles;
    }

    public async Task<Result<string>> UpdatePermissionsAsync(UpdatePermissionRequestGrpc request, CallContext context = default)
    {
        var roles = await _roleService.UpdatePermissionsAsync(request.Id, request.Items);
        return roles;
    }

    [MustHavePermission(PermissionConstants.Roles.Register)]
    public async Task<Result<string>> RegisterRoleAsync(RoleRequest request, CallContext context = default)
    {
        var response = await _roleService.RegisterRoleAsync(request);
        return response;
    }

    [MustHavePermission(PermissionConstants.Roles.Remove)]
    public async Task<Result<string>> DeleteAsync(string id, CallContext context = default)
    {
        var response = await _roleService.DeleteAsync(id);
        return response;
    }
}
