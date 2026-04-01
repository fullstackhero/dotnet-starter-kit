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
    private readonly TimeProvider _timeProvider;

    public TokenService(IOptions<JwtOptions> options, ILogger<TokenService> logger, IdentityMetrics metrics, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
        _timeProvider = timeProvider;
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
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var accessTokenExpiry = now.AddMinutes(_options.AccessTokenMinutes);
        var jwtToken = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: accessTokenExpiry,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        // Refresh token
        var refreshToken = Convert.ToBase64String(Guid.CreateVersion7().ToByteArray());
        var refreshTokenExpiry = now.AddDays(_options.RefreshTokenDays);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Issued JWT for subject {Subject}", subject);
        }
        _metrics.TokenGenerated(subject);

        var response = new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            RefreshTokenExpiresAt: refreshTokenExpiry,
            AccessTokenExpiresAt: accessTokenExpiry);

        return Task.FromResult(response);
    }
}