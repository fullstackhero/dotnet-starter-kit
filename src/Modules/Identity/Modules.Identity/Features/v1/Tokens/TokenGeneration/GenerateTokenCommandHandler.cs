using FSH.Framework.Core.Context;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using Mediator;
using System.Security.Claims;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;

public sealed class GenerateTokenCommandHandler
    : ICommandHandler<GenerateTokenCommand, TokenResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ISecurityAudit _securityAudit;
    private readonly IRequestContext _requestContext;
    private readonly IOutboxStore _outboxStore;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;
    private readonly ISessionService _sessionService;
    private readonly ILogger<GenerateTokenCommandHandler> _logger;

    public GenerateTokenCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ISecurityAudit securityAudit,
        IRequestContext requestContext,
        IOutboxStore outboxStore,
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        ISessionService sessionService,
        ILogger<GenerateTokenCommandHandler> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _securityAudit = securityAudit;
        _requestContext = requestContext;
        _outboxStore = outboxStore;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async ValueTask<TokenResponse> Handle(
        GenerateTokenCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Gather context for auditing
        var ip = _requestContext.IpAddress ?? "unknown";
        var ua = _requestContext.UserAgent ?? "unknown";
        var clientId = _requestContext.ClientId;

        // Validate credentials
        var identityResult = await _identityService
            .ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        if (identityResult is null)
        {
            // 1) Audit failed login BEFORE throwing
            await _securityAudit.LoginFailedAsync(
                subjectIdOrName: request.Email,
                clientId: clientId!,
                reason: "InvalidCredentials",
                ip: ip,
                ct: cancellationToken);

            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        // Unpack subject + claims
        var (subject, claims) = identityResult.Value;

        // 2) Audit successful login
        await _securityAudit.LoginSucceededAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? request.Email,
            clientId: clientId!,
            ip: ip,
            userAgent: ua,
            ct: cancellationToken);

        // Issue token
        var token = await _tokenService.IssueAsync(subject, claims, /*extra*/ null, cancellationToken);

        // Persist refresh token (hashed) for this user
        await _identityService.StoreRefreshTokenAsync(subject, token.RefreshToken, token.RefreshTokenExpiresAt, cancellationToken);

        // Create user session for session management (non-blocking, fail gracefully)
        try
        {
            var refreshTokenHash = Sha256Short(token.RefreshToken);
            await _sessionService.CreateSessionAsync(
                subject,
                refreshTokenHash,
                ip,
                ua,
                token.RefreshTokenExpiresAt,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Session creation is non-critical - don't fail the login
            // This can happen if migrations haven't been applied yet
            _logger.LogWarning(ex, "Failed to create user session for user {UserId}. Login will continue without session tracking.", subject);
        }

        // 3) Audit token issuance with a fingerprint (never raw token)
        var fingerprint = Sha256Short(token.AccessToken);
        await _securityAudit.TokenIssuedAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? request.Email,
            clientId: clientId!,
            tokenFingerprint: fingerprint,
            expiresUtc: token.AccessTokenExpiresAt,
            ct: cancellationToken);

        // 4) Enqueue integration event for token generation (sample event for testing eventing)
        var tenantId = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        var correlationId = Guid.NewGuid().ToString();

        var integrationEvent = new TokenGeneratedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: tenantId,
            CorrelationId: correlationId,
            Source: "Identity",
            UserId: subject,
            Email: request.Email,
            ClientId: clientId!,
            IpAddress: ip,
            UserAgent: ua,
            TokenFingerprint: fingerprint,
            AccessTokenExpiresAtUtc: token.AccessTokenExpiresAt);

        await _outboxStore.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        return token;
    }

    private static string Sha256Short(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        // short printable fingerprint; store only this
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}
