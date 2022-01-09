using DN.WebApi.Application.Identity.Users;
using DN.WebApi.Application.Identity.Users.Password;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Infrastructure.OpenApi;
using DN.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers.Identity;

public sealed class IdentityController : VersionNeutralApiController
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUser _currentUser;
    private readonly IUserService _userService;

    public IdentityController(IIdentityService identityService, ICurrentUser currentUser, IUserService userService)
    {
        _identityService = identityService;
        _currentUser = currentUser;
        _userService = userService;
    }

    [HttpPost("register")]
    [MustHavePermission(FSHPermissions.Identity.Register)]
    public async Task<ActionResult<Result<string>>> RegisterAsync(RegisterUserRequest request)
    {
        string origin = GenerateOrigin();
        return Ok(await _identityService.RegisterAsync(request, origin));
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Search))]
    public async Task<ActionResult<Result<string>>> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string code, [FromQuery] string tenant)
    {
        return Ok(await _identityService.ConfirmEmailAsync(userId, code, tenant));
    }

    [HttpGet("confirm-phone-number")]
    [AllowAnonymous]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Search))]
    public async Task<ActionResult<Result<string>>> ConfirmPhoneNumberAsync([FromQuery] string userId, [FromQuery] string code)
    {
        return Ok(await _identityService.ConfirmPhoneNumberAsync(userId, code));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [TenantKeyHeader]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public async Task<ActionResult<Result>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        string origin = GenerateOrigin();
        return Ok(await _identityService.ForgotPasswordAsync(request, origin));
    }

    [HttpPost("reset-password")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Post))]
    public async Task<ActionResult<Result>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        return Ok(await _identityService.ResetPasswordAsync(request));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<Result>> UpdateProfileAsync(UpdateProfileRequest request)
    {
        return Ok(await _identityService.UpdateProfileAsync(request, _currentUser.GetUserId().ToString()));
    }

    [HttpGet("profile")]
    public async Task<ActionResult<Result<UserDetailsDto>>> GetProfileDetailsAsync()
    {
        return Ok(await _userService.GetAsync(_currentUser.GetUserId().ToString()));
    }

    [HttpPut("change-password")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Put))]
    public async Task<ActionResult<Result>> ChangePasswordAsync(ChangePasswordRequest model)
    {
        var response = await _identityService.ChangePasswordAsync(model, _currentUser.GetUserId().ToString());
        return Ok(response);
    }

    private string GenerateOrigin()
    {
        return $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
    }
}