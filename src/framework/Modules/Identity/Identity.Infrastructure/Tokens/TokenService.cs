using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Auditing.Contracts;
using FSH.Framework.Auditing.Contracts.Enums;
using FSH.Framework.Auditing.Contracts.Events;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Identity.Infrastructure.Users;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FSH.Framework.Identity.Infrastructure.Tokens;
public sealed class TokenService : ITokenService
{
    private readonly UserManager<FshUser> _userManager;
    private readonly IMultiTenantContextAccessor<FshTenantInfo>? _multiTenantContextAccessor;
    private readonly JwtOptions _jwtOptions;
    private readonly IPublisher _publisher;
    public TokenService(IOptions<JwtOptions> jwtOptions, UserManager<FshUser> userManager, IMultiTenantContextAccessor<FshTenantInfo>? multiTenantContextAccessor, IPublisher publisher)
    {
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _publisher = publisher;
    }

    public async Task<TokenDto> GenerateTokenAsync(TokenGenerationRequest request, string ipAddress, CancellationToken cancellationToken)
    {
        var currentTenant = _multiTenantContextAccessor!.MultiTenantContext.TenantInfo;
        if (currentTenant == null) throw new UnauthorizedException();
        if (string.IsNullOrWhiteSpace(currentTenant.Id)
           || await _userManager.FindByEmailAsync(request.Email.Trim().Normalize()) is not { } user
           || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException();
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("user is deactivated");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedException("email not confirmed");
        }

        if (currentTenant.Id != TenantConstants.Root.Id)
        {
            if (!currentTenant.IsActive)
            {
                throw new UnauthorizedException($"tenant {currentTenant.Id} is deactivated");
            }

            if (DateTime.UtcNow > currentTenant.ValidUpto)
            {
                throw new UnauthorizedException($"tenant {currentTenant.Id} validity has expired");
            }
        }

        return await GenerateTokensAndUpdateUser(user, ipAddress);
    }


    public async Task<TokenDto> RefreshTokenAsync(TokenRefreshRequest request, string ipAddress, CancellationToken cancellationToken)
    {
        var userPrincipal = GetPrincipalFromExpiredToken(request.Token);
        var userId = _userManager.GetUserId(userPrincipal)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new UnauthorizedException();
        }

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        return await GenerateTokensAndUpdateUser(user, ipAddress);
    }
    private async Task<TokenDto> GenerateTokensAndUpdateUser(FshUser user, string ipAddress)
    {
        string token = GenerateJwt(user, ipAddress);

        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays);

        await _userManager.UpdateAsync(user);

        await _publisher.Publish(new AuditPublishedEvent(new List<Trail>
        {
            new() {
                Id = Guid.NewGuid(),
                Operation = AuditOperation.Create,
                Description = "Token Generated.",
                EntityName = "Identity",
                UserId = new Guid(user.Id),
                DateTime = DateTime.UtcNow,
            }
        }));


        return new TokenDto(token, user.RefreshToken, user.RefreshTokenExpiryTime);
    }

    private string GenerateJwt(FshUser user, string ipAddress) =>
    GenerateEncryptedToken(GetSigningCredentials(), GetClaims(user, ipAddress));

    private SigningCredentials GetSigningCredentials()
    {
        byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Key);
        return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
    }

    private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(
           claims: claims,
           expires: DateTime.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
           signingCredentials: signingCredentials,
           issuer: JwtAuthConstants.Issuer,
           audience: JwtAuthConstants.Audience
           );
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private List<Claim> GetClaims(FshUser user, string ipAddress) =>
        new()
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FirstName ?? string.Empty),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
            new(FshClaims.Fullname, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new(FshClaims.IpAddress, ipAddress),
            new(FshClaims.Tenant, _multiTenantContextAccessor!.MultiTenantContext.TenantInfo!.Id),
            new(FshClaims.ImageUrl, user.ImageUrl == null ? string.Empty : user.ImageUrl.ToString())
        };
    private static string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
#pragma warning disable CA5404 // Do not disable token validation checks
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = JwtAuthConstants.Audience,
            ValidIssuer = JwtAuthConstants.Issuer,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = false
        };
#pragma warning restore CA5404 // Do not disable token validation checks
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(
                SecurityAlgorithms.HmacSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException("invalid token");
        }

        return principal;
    }
}
