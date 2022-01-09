using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DN.WebApi.Application.Identity;
using DN.WebApi.Application.Identity.Tokens;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Mailing;
using DN.WebApi.Infrastructure.Multitenancy;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Shared.Authorization;
using DN.WebApi.Shared.Multitenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DN.WebApi.Infrastructure.Identity.Services;

public class TokenService : ITokenService
{
    private readonly TenantManagementDbContext _tenantContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<TokenService> _localizer;
    private readonly MailSettings _mailSettings;
    private readonly JwtSettings _jwtSettings;
    private readonly ICurrentTenant _currentTenant;

    public TokenService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        IStringLocalizer<TokenService> localizer,
        IOptions<MailSettings> mailSettings,
        ICurrentTenant currentTenant,
        TenantManagementDbContext tenantContext)
    {
        _userManager = userManager;
        _localizer = localizer;
        _mailSettings = mailSettings.Value;
        _jwtSettings = jwtSettings.Value;
        _currentTenant = currentTenant;
        _tenantContext = tenantContext;
    }

    public async Task<IResult<TokenResponse>> GetTokenAsync(TokenRequest request, string ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim().Normalize());
        if (user == null)
        {
            throw new IdentityException(_localizer["identity.usernotfound"], statusCode: HttpStatusCode.Unauthorized);
        }

        string? tenant = user.Tenant;
        if (tenant != MultitenancyConstants.Root.Key)
        {
            var tenantInfo = await _tenantContext.Tenants.Where(a => a.Key == tenant).FirstOrDefaultAsync();
            if (tenantInfo is null)
            {
                throw new InvalidTenantException(_localizer["tenant.invalid"]);
            }

            if (!tenantInfo.IsActive)
            {
                throw new InvalidTenantException(_localizer["tenant.inactive"]);
            }

            if (DateTime.UtcNow > tenantInfo.ValidUpto)
            {
                throw new InvalidTenantException(_localizer["tenant.expired"]);
            }
        }

        if (!user.IsActive)
        {
            throw new IdentityException(_localizer["identity.usernotactive"], statusCode: HttpStatusCode.Unauthorized);
        }

        if (_mailSettings.EnableVerification && !user.EmailConfirmed)
        {
            throw new IdentityException(_localizer["identity.emailnotconfirmed"], statusCode: HttpStatusCode.Unauthorized);
        }

        bool passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            throw new IdentityException(_localizer["identity.invalidcredentials"], statusCode: HttpStatusCode.Unauthorized);
        }

        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
        await _userManager.UpdateAsync(user);
        string token = GenerateJwt(user, ipAddress);
        var response = new TokenResponse(token, user.RefreshToken, user.RefreshTokenExpiryTime);
        return await Result<TokenResponse>.SuccessAsync(response);
    }

    public async Task<IResult<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress)
    {
        if (request is null)
        {
            throw new IdentityException(_localizer["identity.invalidtoken"], statusCode: HttpStatusCode.Unauthorized);
        }

        var userPrincipal = GetPrincipalFromExpiredToken(request.Token);
        string userEmail = userPrincipal.FindFirstValue(ClaimTypes.Email);
        var user = await _userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            throw new IdentityException(_localizer["identity.usernotfound"], statusCode: HttpStatusCode.NotFound);
        }

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new IdentityException(_localizer["identity.invalidrefreshtoken"], statusCode: HttpStatusCode.Unauthorized);
        }

        string token = GenerateEncryptedToken(GetSigningCredentials(), GetClaims(user, ipAddress));
        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
        await _userManager.UpdateAsync(user);
        var response = new TokenResponse(token, user.RefreshToken, user.RefreshTokenExpiryTime);
        return await Result<TokenResponse>.SuccessAsync(response);
    }

    private string GenerateJwt(ApplicationUser user, string ipAddress)
    {
        return GenerateEncryptedToken(GetSigningCredentials(), GetClaims(user, ipAddress));
    }

    private IEnumerable<Claim> GetClaims(ApplicationUser user, string ipAddress) =>
        new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(FSHClaims.Fullname, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Name, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new(FSHClaims.IpAddress, ipAddress),
            new(FSHClaims.Tenant, _currentTenant.Key),
            new(FSHClaims.ImageUrl, user.ImageUrl ?? string.Empty),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty)
        };

    private string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(
           claims: claims,
           expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpirationInMinutes),
           signingCredentials: signingCredentials);
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrEmpty(_jwtSettings.Key))
        {
            throw new InvalidOperationException("No Key defined in JwtSettings config.");
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = false
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(
                SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new IdentityException(_localizer["identity.invalidtoken"], statusCode: HttpStatusCode.Unauthorized);
        }

        return principal;
    }

    private SigningCredentials GetSigningCredentials()
    {
        if (string.IsNullOrEmpty(_jwtSettings.Key))
        {
            throw new InvalidOperationException("No Key defined in JwtSettings config.");
        }

        byte[] secret = Encoding.UTF8.GetBytes(_jwtSettings.Key);
        return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
    }
}