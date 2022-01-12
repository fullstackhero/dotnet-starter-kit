using FSH.WebApi.Application.Identity.Roles;

namespace FSH.WebApi.Host.Controllers.Identity;

public class RolesController : VersionNeutralApiController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService) => _roleService = roleService;

    [HttpGet("all")]
    [MustHavePermission(FSHPermissions.Roles.ListAll)]
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
    public Task<List<PermissionDto>> GetPermissionsAsync(string id, CancellationToken cancellationToken)
    {
        return _roleService.GetPermissionsAsync(id, cancellationToken);
    }

    [HttpPut("{id}/permissions")]
    [MustHavePermission(FSHPermissions.RoleClaims.Edit)]
    public Task<string> UpdatePermissionsAsync(string id, List<UpdatePermissionsRequest> request, CancellationToken cancellationToken)
    {
        return _roleService.UpdatePermissionsAsync(id, request, cancellationToken);
    }

    [HttpPost]
    [MustHavePermission(FSHPermissions.Roles.Register)]
    public Task<string> RegisterRoleAsync(RoleRequest request)
    {
        return _roleService.RegisterRoleAsync(request);
    }

    [HttpDelete("{id}")]
    [MustHavePermission(FSHPermissions.Roles.Remove)]
    public Task<string> DeleteAsync(string id)
    {
        return _roleService.DeleteAsync(id);
    }
}