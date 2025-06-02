using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using FSH.Framework.Infrastructure.Auth;
using Microsoft.IdentityModel.Tokens;

namespace FSH.Starter.WebApi.Host;

public static class JwtHelper
{
    private const string Issuer = "https://fullstackhero.net";
    private const string Audience = "fullstackhero";

    public static string GenerateToken(User user, string secretKey, IReadOnlyList<string>? roles = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new (ClaimTypes.NameIdentifier, user.id.ToString()),
            new (ClaimTypes.Email, user.email),
            new (ClaimTypes.Name, $"{user.first_name} {user.last_name}")
        };

        // Add role claims if provided
        if (roles != null && roles.Count > 0)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static ClaimsPrincipal? ValidateToken(string token, string secretKey)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<string> GetRolesFromToken(string token, string secretKey)
    {
        var principal = ValidateToken(token, secretKey);
        return principal?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? new List<string>();
    }
} 
