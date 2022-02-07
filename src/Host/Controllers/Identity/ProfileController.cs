using System.Security.Claims;
using FSH.WebApi.Application.Identity.Users;
using FSH.WebApi.Application.Identity.Users.Profile;

namespace FSH.WebApi.Host.Controllers.Identity;

public sealed class ProfileController : VersionNeutralApiController
{
    private readonly IProfileService _profileService;
    private readonly IUserService _userService;

    public ProfileController(IProfileService profileService, IUserService userService) =>
        (_profileService, _userService) = (profileService, userService);

    [HttpGet]
    [OpenApiOperation("Get profile details of currently logged in user.", "")]
    public async Task<ActionResult<UserDetailsDto>> GetAsync(CancellationToken cancellationToken)
    {
        return User.GetUserId() is not { } userId || string.IsNullOrEmpty(userId)
            ? Unauthorized()
            : Ok(await _userService.GetAsync(userId, cancellationToken));
    }

    [HttpPost]
    [AllowAnonymous]
    [OpenApiOperation("Create a new profile.", "")]
    public Task<string> CreateAsync(CreateProfileRequest request)
    {
        // TODO: check if registering anonymous users is actually allowed (should probably be an appsetting)
        // and return UnAuthorized when it isn't
        // Also: add other protection to prevent automatic posting (captcha?)
        return _profileService.CreateAsync(request, GetOriginFromRequest());
    }

    [HttpPut]
    [OpenApiOperation("Update an existing profile.", "")]
    public async Task<ActionResult> UpdateAsync(UpdateProfileRequest request)
    {
        if (User.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _profileService.UpdateAsync(request, userId);
        return Ok();
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [OpenApiOperation("Confirm email address for a profile.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Search))]
    public Task<string> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string code, [FromQuery] string tenant, CancellationToken cancellationToken)
    {
        return _profileService.ConfirmEmailAsync(userId, code, tenant, cancellationToken);
    }

    [HttpGet("confirm-phone-number")]
    [AllowAnonymous]
    [OpenApiOperation("Confirm phone number for a profile.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Search))]
    public Task<string> ConfirmPhoneNumberAsync([FromQuery] string userId, [FromQuery] string code)
    {
        return _profileService.ConfirmPhoneNumberAsync(userId, code);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [TenantIdHeader]
    [OpenApiOperation("Request a pasword reset email.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        return _profileService.ForgotPasswordAsync(request, GetOriginFromRequest());
    }

    [HttpPost("reset-password")]
    [OpenApiOperation("Reset your password.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        return _profileService.ResetPasswordAsync(request);
    }

    [HttpPut("change-password")]
    [OpenApiOperation("Change your password.", "")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public async Task<ActionResult> ChangePasswordAsync(ChangePasswordRequest model)
    {
        if (User.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _profileService.ChangePasswordAsync(model, userId);
        return Ok();
    }

    private string GetOriginFromRequest() => $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
}