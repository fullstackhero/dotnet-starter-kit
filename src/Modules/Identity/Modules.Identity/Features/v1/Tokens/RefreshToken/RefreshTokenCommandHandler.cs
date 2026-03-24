using FSH.Framework.Core.Context;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using Mediator;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FSH.Modules.Identity.Features.v1.Tokens.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, RefreshTokenCommandResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ISecurityAudit _securityAudit;
    private readonly IRequestContext _requestContext;
    private readonly ISessionService _sessionService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ISecurityAudit securityAudit,
        IRequestContext requestContext,
        ISessionService sessionService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _securityAudit = securityAudit;
        _requestContext = requestContext;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async ValueTask<RefreshTokenCommandResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var clientId = _requestContext.ClientId;

        // Validate refresh token and rebuild subject + claims
        var validated = await _identityService
            .ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (validated is null)
        {
            await _securityAudit.TokenRevokedAsync("unknown", clientId!, "InvalidRefreshToken", cancellationToken);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var (subject, claims) = validated.Value;

        // Check if the session associated with this refresh token is still valid
        var refreshTokenHash = Sha256Short(request.RefreshToken);
        var isSessionValid = await _sessionService.ValidateSessionAsync(refreshTokenHash, cancellationToken);
        if (!isSessionValid)
        {
            await _securityAudit.TokenRevokedAsync(subject, clientId!, "SessionRevoked", cancellationToken);
            throw new UnauthorizedAccessException("Session has been revoked.");
        }

        // Optionally, cross-check the provided access token subject
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken? parsedAccessToken = null;
        try
        {
            parsedAccessToken = handler.ReadJwtToken(request.Token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse access token during refresh; relying on refresh-token validation only");
        }

        if (parsedAccessToken is not null)
        {
            var accessTokenSubject = parsedAccessToken.Subject;

            if (!string.IsNullOrEmpty(accessTokenSubject) &&
                !string.Equals(accessTokenSubject, subject, StringComparison.Ordinal))
            {
                await _securityAudit.TokenRevokedAsync(subject, clientId!, "RefreshTokenSubjectMismatch", cancellationToken);
                throw new UnauthorizedAccessException("Access token subject mismatch.");
            }
        }

        // Audit previous token revocation by rotation (no raw tokens)
        await _securityAudit.TokenRevokedAsync(subject, clientId!, "RefreshTokenRotated", cancellationToken);

        // Issue new tokens
        var newToken = await _tokenService.IssueAsync(subject, claims, null, cancellationToken);

        // Persist rotated refresh token for this user
        await _identityService.StoreRefreshTokenAsync(subject, newToken.RefreshToken, newToken.RefreshTokenExpiresAt, cancellationToken);

        // Update the session with the new refresh token hash
        var newRefreshTokenHash = Sha256Short(newToken.RefreshToken);
        await _sessionService.UpdateSessionRefreshTokenAsync(
            refreshTokenHash,
            newRefreshTokenHash,
            newToken.RefreshTokenExpiresAt,
            cancellationToken);

        // Audit the newly issued token with a fingerprint
        var fingerprint = Sha256Short(newToken.AccessToken);
        await _securityAudit.TokenIssuedAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty,
            clientId: clientId!,
            tokenFingerprint: fingerprint,
            expiresUtc: newToken.AccessTokenExpiresAt,
            ct: cancellationToken);

        return new RefreshTokenCommandResponse(
            Token: newToken.AccessToken,
            RefreshToken: newToken.RefreshToken,
            RefreshTokenExpiryTime: newToken.RefreshTokenExpiresAt);
    }

    private static string Sha256Short(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}
