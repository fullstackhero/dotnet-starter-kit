using FSH.Modules.Identity.Authorization.Jwt;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FSH.Modules.Identity.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly ILogger<TokenService> _logger;
    private readonly IdentityMetrics _metrics;

    public TokenService(IOptions<JwtOptions> options, ILogger<TokenService> logger, IdentityMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public Task<TokenResponse> IssueAsync(
        string subject,
        IEnumerable<Claim> claims,
        string? tenant = null,
        CancellationToken ct = default)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Access token
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var jwtToken = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: accessTokenExpiry,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        // Refresh token
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);

        var userEmail = claims.Where(a => a.Type == ClaimTypes.Email).Select(a => a.Value).First();
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Issued JWT for user {EmailHash}", MaskEmail(userEmail));
        }
        _metrics.TokenGenerated(userEmail);

        var response = new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            RefreshTokenExpiresAt: refreshTokenExpiry,
            AccessTokenExpiresAt: accessTokenExpiry);

        return Task.FromResult(response);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "unknown";
        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 1) return "***" + email[atIndex..];
        return string.Concat(email.AsSpan(0, 1), "***", email.AsSpan(atIndex));
    }
}