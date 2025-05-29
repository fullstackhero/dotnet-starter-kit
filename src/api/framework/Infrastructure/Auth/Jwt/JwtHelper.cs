using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FSH.Framework.Infrastructure.Auth;

public static class JwtHelper
{
    public static string GenerateToken(User user, string secretKey, int expireDays = 7)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.email ?? string.Empty),
            new Claim("firstName", user.first_name ?? string.Empty),
            new Claim("lastName", user.last_name ?? string.Empty),
            new Claim("status", user.status ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "your-app",
            audience: "your-app",
            claims: claims,
            expires: DateTime.Now.AddDays(expireDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
} 