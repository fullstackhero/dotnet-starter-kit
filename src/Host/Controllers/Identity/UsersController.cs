using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.Authorization;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using DN.WebApi.Application.Identity.Users;
using DN.WebApi.Application.Identity;

namespace DN.WebApi.Host.Controllers.Identity;

public class UsersController : VersionNeutralApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [MustHavePermission(FSHPermissions.Users.View)]
    public async Task<ActionResult<Result<List<UserDetailsDto>>>> GetAllAsync()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [MustHavePermission(FSHPermissions.Users.View)]
    public async Task<ActionResult<Result<UserDetailsDto>>> GetByIdAsync(string id)
    {
        var user = await _userService.GetAsync(id);
        return Ok(user);
    }

    [HttpGet("{id}/roles")]
    [MustHavePermission(FSHPermissions.Roles.View)]
    public async Task<ActionResult<Result<UserRolesResponse>>> GetRolesAsync(string id)
    {
        var userRoles = await _userService.GetRolesAsync(id);
        return Ok(userRoles);
    }

    [HttpGet("{id}/permissions")]
    [MustHavePermission(FSHPermissions.RoleClaims.View)]
    public async Task<ActionResult<Result<List<PermissionDto>>>> GetPermissionsAsync(string id)
    {
        var userPermissions = await _userService.GetPermissionsAsync(id);
        return Ok(userPermissions);
    }

    [HttpPost("{id}/roles")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public async Task<ActionResult<Result<string>>> AssignRolesAsync(string id, UserRolesRequest request)
    {
        var result = await _userService.AssignRolesAsync(id, request);
        return Ok(result);
    }

    [HttpPost("toggle-status")]
    [MustHavePermission(FSHPermissions.Users.Edit)]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public async Task<IActionResult> ToggleUserStatusAsync(ToggleUserStatusRequest request)
    {
        return Ok(await _userService.ToggleUserStatusAsync(request));
    }
}