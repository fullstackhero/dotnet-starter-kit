using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Infrastructure.SwaggerFilters;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class TokensController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public TokensController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        [HttpPost]
        [AllowAnonymous]
        [SwaggerHeader("tenantKey", "Input your tenant Id to access this API", "", true)]
        [SwaggerOperation(Summary = "Submit Credentials with Tenant Key to generate valid Access Token.")]
        public async Task<IActionResult> GetTokenAsync(TokenRequest request, [FromHeader(Name = "tenantKey")][Required] string tenantKey = null)
        {
            var token = await _tokenService.GetTokenAsync(request, GenerateIPAddress());
            return Ok(token);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [SwaggerHeader("tenantKey", "Input your tenant Id to access this API", "", true)]
        public async Task<ActionResult> RefreshAsync(RefreshTokenRequest request, [FromHeader(Name = "tenantKey")][Required] string tenantKey = null)
        {
            var response = await _tokenService.RefreshTokenAsync(request, GenerateIPAddress());
            return Ok(response);
        }

        private string GenerateIPAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"];
            }
            else
            {
                return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
            }
        }
    }
}