using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FSH.Framework.Core.Configurations;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Tokens;
using FSH.Framework.Core.Identity.Tokens.Features.Generate;
using FSH.Framework.Infrastructure.Identity.Users;
using FSH.Framework.Infrastructure.Tenant;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FSH.Framework.Infrastructure.Identity.Tokens;
internal class TokenService(
        UserManager<FshUser> userManager,
        FshTenantInfo? currentTenant,
        IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions jwt = jwtOptions.Value;
    public async Task<TokenGenerationResponse> GenerateTokenAsync(
        TokenGenerationCommand request,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        if (currentTenant == null) throw new UnauthorizedException("authentication failed.");
        if (string.IsNullOrWhiteSpace(currentTenant.Id)
           || await userManager.FindByEmailAsync(request.Email.Trim().Normalize()) is not { } user
           || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("authentication failed.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User Not Active. Please contact the administrator.");
        }

        //if (_securitySettings.RequireConfirmedAccount && !user.EmailConfirmed)
        //{
        //    throw new UnauthorizedException("E-Mail not confirmed.");
        //}

        if (currentTenant.Id != IdentityConstants.RootTenant)
        {
            if (!currentTenant.IsActive)
            {
                throw new UnauthorizedException("Tenant is not Active. Please contact the Application Administrator.");
            }

            if (DateTime.UtcNow > currentTenant.ValidUpto)
            {
                throw new UnauthorizedException("Tenant Validity Has Expired. Please contact the Application Administrator.");
            }
        }

        return await GenerateTokensAndUpdateUser(user, ipAddress);
    }
    private async Task<TokenGenerationResponse> GenerateTokensAndUpdateUser(FshUser user, string ipAddress)
    {
        string token = GenerateJwt(user, ipAddress);

        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(jwt.RefreshTokenExpirationInDays);

        await userManager.UpdateAsync(user);

        return new TokenGenerationResponse(token, user.RefreshToken, user.RefreshTokenExpiryTime);
    }

    private string GenerateJwt(FshUser user, string ipAddress) =>
    GenerateEncryptedToken(GetSigningCredentials(), GetClaims(user, ipAddress));

    private SigningCredentials GetSigningCredentials()
    {
        byte[] secret = Encoding.UTF8.GetBytes(jwt.Key);
        return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
    }

    private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(
           claims: claims,
           expires: DateTime.UtcNow.AddMinutes(jwt.TokenExpirationInMinutes),
           signingCredentials: signingCredentials);
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private List<Claim> GetClaims(FshUser user, string ipAddress) =>
        new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(IdentityConstants.Claims.Fullname, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Name, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new(IdentityConstants.Claims.IpAddress, ipAddress),
            new(IdentityConstants.Claims.Tenant, currentTenant!.Id),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty)
        };
    private static string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
