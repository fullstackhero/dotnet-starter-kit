using FSH.WebApi.Application.Identity.Roles;
using FSH.WebApi.Application.Identity.Users;

namespace FSH.WebApi.Host.Controllers.Identity;

public class UsersController : VersionNeutralApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet]
    [MustHavePermission(FSHPermissions.Users.View)]
    public Task<List<UserDetailsDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _userService.GetAllAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    [MustHavePermission(FSHPermissions.Users.View)]
    public Task<UserDetailsDto> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return _userService.GetAsync(id, cancellationToken);
    }

    [HttpGet("{id}/roles")]
    [MustHavePermission(FSHPermissions.Roles.View)]
    public Task<List<UserRoleDto>> GetRolesAsync(string id, CancellationToken cancellationToken)
    {
        return _userService.GetRolesAsync(id, cancellationToken);
    }

    [HttpGet("{id}/permissions")]
    [MustHavePermission(FSHPermissions.RoleClaims.View)]
    public Task<List<PermissionDto>> GetPermissionsAsync(string id, CancellationToken cancellationToken)
    {
        return _userService.GetPermissionsAsync(id, cancellationToken);
    }

    [HttpPost("{id}/roles")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> AssignRolesAsync(string id, UserRolesRequest request, CancellationToken cancellationToken)
    {
        return _userService.AssignRolesAsync(id, request, cancellationToken);
    }

    [HttpPost("toggle-status")]
    [MustHavePermission(FSHPermissions.Users.Update)]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task ToggleUserStatusAsync(ToggleUserStatusRequest request, CancellationToken cancellationToken)
    {
        return _userService.ToggleUserStatusAsync(request, cancellationToken);
    }
}