using FSH.WebApi.Application.Identity.Roles;

namespace FSH.WebApi.Host.Controllers.Identity;

public class RolesController : VersionNeutralApiController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService) => _roleService = roleService;

    [HttpGet("all")]
    [MustHavePermission(FSHPermissions.Roles.View)]
    public Task<List<RoleDto>> GetListAsync()
    {
        return _roleService.GetListAsync();
    }

    [HttpGet("{id}")]
    [MustHavePermission(FSHPermissions.Roles.View)]
    public Task<RoleDto> GetByIdAsync(string id)
    {
        return _roleService.GetByIdAsync(id);
    }

    [HttpGet("{id}/permissions")]
    [MustHavePermission(FSHPermissions.RoleClaims.View)]
    public Task<RoleDto> GetByIdWithPermissionsAsync(string id, CancellationToken cancellationToken)
    {
        return _roleService.GetByIdWithPermissionsAsync(id, cancellationToken);
    }

    [HttpPut("permissions")]
    [MustHavePermission(FSHPermissions.RoleClaims.Update)]
    public Task<string> UpdatePermissionsAsync(UpdatePermissionsRequest request, CancellationToken cancellationToken)
    {
        return _roleService.UpdatePermissionsAsync(request, cancellationToken);
    }

    [HttpPost]
    [MustHavePermission(FSHPermissions.Roles.Create)]
    public Task<string> RegisterRoleAsync(RoleRequest request)
    {
        return _roleService.RegisterRoleAsync(request);
    }

    [HttpDelete("{id}")]
    [MustHavePermission(FSHPermissions.Roles.Delete)]
    public Task<string> DeleteAsync(string id)
    {
        return _roleService.DeleteAsync(id);
    }
}