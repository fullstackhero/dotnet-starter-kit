using FSH.WebApi.Application.Identity.Users;

namespace FSH.WebApi.Host.Controllers.Identity;

public class UsersController : VersionNeutralApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet]
    [MustHavePermission(FSHAction.View, FSHResource.Users)]
    public Task<List<UserDetailsDto>> GetListAsync(CancellationToken cancellationToken)
    {
        return _userService.GetListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    [MustHavePermission(FSHAction.View, FSHResource.Users)]
    public Task<UserDetailsDto> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return _userService.GetAsync(id, cancellationToken);
    }

    [HttpGet("{id}/roles")]
    [MustHavePermission(FSHAction.View, FSHResource.UserRoles)]
    public Task<List<UserRoleDto>> GetRolesAsync(string id, CancellationToken cancellationToken)
    {
        return _userService.GetRolesAsync(id, cancellationToken);
    }

    [HttpPost("{id}/roles")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    [MustHavePermission(FSHAction.Update, FSHResource.UserRoles)]
    public Task<string> AssignRolesAsync(string id, UserRolesRequest request, CancellationToken cancellationToken)
    {
        return _userService.AssignRolesAsync(id, request, cancellationToken);
    }

    [HttpPost("toggle-status")]
    [MustHavePermission(FSHAction.Update, FSHResource.Users)]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task ToggleUserStatusAsync(ToggleUserStatusRequest request, CancellationToken cancellationToken)
    {
        return _userService.ToggleUserStatusAsync(request, cancellationToken);
    }
}