using FSH.WebApi.Application.Identity.Users;

namespace FSH.WebApi.Host.Controllers.Identity;

public sealed class IdentityController : VersionNeutralApiController
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUser _currentUser;
    private readonly IUserService _userService;

    private string OriginFromRequest => $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";

    public IdentityController(IIdentityService identityService, ICurrentUser currentUser, IUserService userService)
    {
        _identityService = identityService;
        _currentUser = currentUser;
        _userService = userService;
    }

    [HttpPost("register")]
    [MustHavePermission(FSHPermissions.Identity.Create)]
    public Task<string> RegisterAsync(RegisterUserRequest request)
    {
        return _identityService.RegisterAsync(request, OriginFromRequest);
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Search))]
    public Task<string> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string code, [FromQuery] string tenant, CancellationToken cancellationToken)
    {
        return _identityService.ConfirmEmailAsync(userId, code, tenant, cancellationToken);
    }

    [HttpGet("confirm-phone-number")]
    [AllowAnonymous]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Search))]
    public Task<string> ConfirmPhoneNumberAsync([FromQuery] string userId, [FromQuery] string code)
    {
        return _identityService.ConfirmPhoneNumberAsync(userId, code);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [TenantIdHeader]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        return _identityService.ForgotPasswordAsync(request, OriginFromRequest);
    }

    [HttpPost("reset-password")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        return _identityService.ResetPasswordAsync(request);
    }

    [HttpPut("profile")]
    public Task UpdateProfileAsync(UpdateProfileRequest request)
    {
        return _identityService.UpdateProfileAsync(request, _currentUser.GetUserId().ToString());
    }

    [HttpGet("profile")]
    public Task<UserDetailsDto> GetProfileDetailsAsync(CancellationToken cancellationToken)
    {
        return _userService.GetAsync(_currentUser.GetUserId().ToString(), cancellationToken);
    }

    [HttpPut("change-password")]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
    public Task ChangePasswordAsync(ChangePasswordRequest model)
    {
        return _identityService.ChangePasswordAsync(model, _currentUser.GetUserId().ToString());
    }
}