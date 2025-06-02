using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FSH.Framework.Infrastructure.Auth;

public static class JwtHelper
{
    public static string GenerateToken(User user, string secretKey, IReadOnlyList<string> roles, int expireDays = 7)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.email ?? string.Empty),
            new Claim("email", user.email ?? string.Empty),
            new Claim("username", user.username ?? string.Empty),
            new Claim("first_name", user.first_name ?? string.Empty),
            new Claim("last_name", user.last_name ?? string.Empty),
            new Claim("phone_number", user.phone_number ?? string.Empty),
            new Claim("profession", user.profession ?? string.Empty),
            new Claim("status", user.status ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles to claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtAuthConstants.Issuer,
            audience: JwtAuthConstants.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expireDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateToken(User user, string secretKey, int expireDays = 7)
    {
        return GenerateToken(user, secretKey, new List<string>(), expireDays);
    }
}

public static class JwtAuthConstants
{
    public const string Issuer = "https://fullstackhero.net";
    public const string Audience = "fullstackhero";
} 