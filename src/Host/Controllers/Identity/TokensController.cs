using DN.WebApi.Application.Identity.Tokens;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Infrastructure.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Host.Controllers.Identity;

public sealed class TokensController : VersionNeutralApiController
{
    private readonly ITokenService _tokenService;

    public TokensController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost]
    [AllowAnonymous]
    [TenantKeyHeader]
    [OpenApiOperation("Submit Credentials with Tenant Key to generate valid Access Token.", "")]
    public async Task<ActionResult<Result<TokenResponse>>> GetTokenAsync(TokenRequest request)
    {
        var token = await _tokenService.GetTokenAsync(request, GenerateIpAddress());
        return Ok(token);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [TenantKeyHeader]
    [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Search))]
    public async Task<ActionResult<Result<TokenResponse>>> RefreshAsync(RefreshTokenRequest request)
    {
        var response = await _tokenService.RefreshTokenAsync(request, GenerateIpAddress());
        return Ok(response);
    }

    private string GenerateIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"];
        }
        else
        {
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
        }
    }
}