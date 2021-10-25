using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class IdentityController : ControllerBase
    {
        private readonly ICurrentUser _user;
        private readonly IIdentityService _identityService;
        private readonly IUserService _userService;

        public IdentityController(IIdentityService identityService, ICurrentUser user, IUserService userService)
        {
            _identityService = identityService;
            _user = user;
            _userService = userService;
        }

        [HttpPost("register")]
        [MustHavePermission(PermissionConstants.Identity.Register)]
        public async Task<IActionResult> RegisterAsync(RegisterRequest request)
        {
            string baseUrl = $"{this.Request.Scheme}://{this.Request.Host.Value.ToString()}{this.Request.PathBase.Value.ToString()}";
            string origin = string.IsNullOrEmpty(Request.Headers["origin"].ToString()) ? baseUrl : Request.Headers["origin"].ToString();
            return Ok(await _identityService.RegisterAsync(request, origin));
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string code, [FromQuery] string tenantKey)
        {
            return Ok(await _identityService.ConfirmEmailAsync(userId, code, tenantKey));
        }

        [HttpGet("confirm-phone-number")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmPhoneNumberAsync([FromQuery] string userId, [FromQuery] string code)
        {
            return Ok(await _identityService.ConfirmPhoneNumberAsync(userId, code));
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var origin = Request.Headers["origin"];
            return Ok(await _identityService.ForgotPasswordAsync(request, origin));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordAsync(ResetPasswordRequest request)
        {
            return Ok(await _identityService.ResetPasswordAsync(request));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfileAsync(UpdateProfileRequest request)
        {
            return Ok(await _identityService.UpdateProfileAsync(request, _user.GetUserId().ToString()));
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfileDetailsAsync()
        {
            return Ok(await _userService.GetAsync(_user.GetUserId().ToString()));
        }
    }
}