using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using Mediator;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;

public sealed class GenerateTokenCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    ISecurityAudit securityAudit,
    IRequestContext requestContext,
    IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ISessionService sessionService,
    ILogger<GenerateTokenCommandHandler> logger)
        : ICommandHandler<GenerateTokenCommand, TokenResponse>
{
    public async ValueTask<TokenResponse> Handle(
        GenerateTokenCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Gather context for auditing
        var ip = requestContext.IpAddress ?? "unknown";
        var ua = requestContext.UserAgent ?? "unknown";
        var clientId = requestContext.ClientId;

        // Validate credentials
        var identityResult = await identityService
            .ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        if (identityResult is null)
        {
            // 1) Audit failed login BEFORE throwing
            await securityAudit.LoginFailedAsync(
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
        await securityAudit.LoginSucceededAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? request.Email,
            clientId: clientId!,
            ip: ip,
            userAgent: ua,
            ct: cancellationToken);

        // Issue token
        var token = await tokenService.IssueAsync(subject, claims, /*extra*/ null, cancellationToken);

        // Persist refresh token (hashed) for this user
        await identityService.StoreRefreshTokenAsync(subject, token.RefreshToken, token.RefreshTokenExpiresAt, cancellationToken);

        // Create user session for session management (non-blocking, fail gracefully)
        try
        {
            var refreshTokenHash = Sha256Short(token.RefreshToken);
            await sessionService.CreateSessionAsync(
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
            logger.LogWarning(ex, "Failed to create user session for user {UserId}. Login will continue without session tracking.", subject);
        }

        // 3) Audit token issuance with a fingerprint (never raw token)
        var fingerprint = Sha256Short(token.AccessToken);
        await securityAudit.TokenIssuedAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? request.Email,
            clientId: clientId!,
            tokenFingerprint: fingerprint,
            expiresUtc: token.AccessTokenExpiresAt,
            ct: cancellationToken);

        // 4) Enqueue integration event for token generation (sample event for testing eventing)
        var tenantId = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        var correlationId = Guid.CreateVersion7().ToString();

        var integrationEvent = new TokenGeneratedIntegrationEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
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

        await outboxStore.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        return token;
    }

    private static string Sha256Short(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        // short printable fingerprint; store only this
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}