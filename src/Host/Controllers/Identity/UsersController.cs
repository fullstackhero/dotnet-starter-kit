using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers.Identity;

[ApiController]
[Route("api/[controller]")]
[ApiVersionNeutral]
[ApiConventionType(typeof(FSHApiConventions))]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<UserDetailsDto>>>> GetAllAsync()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<UserDetailsDto>>> GetByIdAsync(string id)
    {
        var user = await _userService.GetAsync(id);
        return Ok(user);
    }

    [HttpGet("{id}/roles")]
    public async Task<ActionResult<Result<UserRolesResponse>>> GetRolesAsync(string id)
    {
        var userRoles = await _userService.GetRolesAsync(id);
        return Ok(userRoles);
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<Result<List<PermissionDto>>>> GetPermissionsAsync(string id)
    {
        var userPermissions = await _userService.GetPermissionsAsync(id);
        return Ok(userPermissions);
    }

    [HttpPost("{id}/roles")]
    [ProducesResponseType(200)]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    public async Task<ActionResult<Result<string>>> AssignRolesAsync(string id, UserRolesRequest request)
    {
        var result = await _userService.AssignRolesAsync(id, request);
        return Ok(result);
    }

    [HttpPost("toggle-status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400, Type = typeof(HttpValidationProblemDetails))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    public async Task<IActionResult> ToggleUserStatusAsync(ToggleUserStatusRequest request)
    {
        return Ok(await _userService.ToggleUserStatusAsync(request));
    }
}