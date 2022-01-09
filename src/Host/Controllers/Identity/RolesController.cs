using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.Authorization;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using DN.WebApi.Application.Identity.Roles;
using DN.WebApi.Application.Identity;

namespace DN.WebApi.Host.Controllers.Identity;

public class RolesController : VersionNeutralApiController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("all")]
    [MustHavePermission(FSHPermissions.Roles.ListAll)]
    public async Task<ActionResult<Result<List<RoleDto>>>> GetListAsync()
    {
        var roles = await _roleService.GetListAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    [MustHavePermission(FSHPermissions.Roles.View)]
    public async Task<ActionResult<Result<RoleDto>>> GetByIdAsync(string id)
    {
        var roles = await _roleService.GetByIdAsync(id);
        return Ok(roles);
    }

    [HttpGet("{id}/permissions")]
    [MustHavePermission(FSHPermissions.RoleClaims.View)]
    public async Task<ActionResult<Result<List<PermissionDto>>>> GetPermissionsAsync(string id)
    {
        var roles = await _roleService.GetPermissionsAsync(id);
        return Ok(roles);
    }

    [HttpPut("{id}/permissions")]
    [MustHavePermission(FSHPermissions.RoleClaims.Edit)]
    public async Task<ActionResult<Result<string>>> UpdatePermissionsAsync(string id, List<UpdatePermissionsRequest> request)
    {
        var roles = await _roleService.UpdatePermissionsAsync(id, request);
        return Ok(roles);
    }

    [HttpPost]
    [MustHavePermission(FSHPermissions.Roles.Register)]
    public async Task<ActionResult<Result<string>>> RegisterRoleAsync(RoleRequest request)
    {
        var response = await _roleService.RegisterRoleAsync(request);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [MustHavePermission(FSHPermissions.Roles.Remove)]
    public async Task<ActionResult<Result<string>>> DeleteAsync(string id)
    {
        var response = await _roleService.DeleteAsync(id);
        return Ok(response);
    }
}