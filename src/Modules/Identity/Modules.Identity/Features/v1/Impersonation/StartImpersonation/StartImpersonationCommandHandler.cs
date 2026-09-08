using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Impersonation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;
using FSH.Modules.Identity.Localization;
using Mediator;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FSH.Modules.Identity.Features.v1.Impersonation.StartImpersonation;

public sealed class StartImpersonationCommandHandler
    : ICommandHandler<StartImpersonationCommand, ImpersonationResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ISecurityAudit _securityAudit;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;
    private readonly IImpersonationGrantService _grantService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StartImpersonationCommandHandler> _logger;

    public StartImpersonationCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ISecurityAudit securityAudit,
        ICurrentUser currentUser,
        IRequestContext requestContext,
        IImpersonationGrantService grantService,
        TimeProvider timeProvider,
        ILogger<StartImpersonationCommandHandler> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _securityAudit = securityAudit;
        _currentUser = currentUser;
        _requestContext = requestContext;
        _grantService = grantService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async ValueTask<ImpersonationResponse> Handle(
        StartImpersonationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }

        var actorUserId = _currentUser.GetUserId().ToString();
        var actorTenantId = _currentUser.GetTenant()
            ?? throw new UnauthorizedException("missing tenant context")
            {
                MessageKey = "Error.InvalidTenant",
            };
        var actorUserName = _currentUser.Name;

        // Cross-tenant impersonation requires the actor to be in the root tenant. Tenant admins
        // can only impersonate users within their own tenant.
        if (!string.Equals(actorTenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal)
            && !string.Equals(actorTenantId, request.TargetTenantId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("cross-tenant impersonation is restricted to platform operators")
            {
                MessageKey = "Identity.CrossTenantImpersonationRestricted",
                ResourceSource = typeof(IdentityResources),
            };
        }

        // Prevent self-impersonation (pointless, confuses the audit trail). Caller error → explicit 4xx,
        // not the 500 CustomException defaults to.
        if (string.Equals(actorUserId, request.TargetUserId, StringComparison.Ordinal)
            && string.Equals(actorTenantId, request.TargetTenantId, StringComparison.Ordinal))
        {
            throw new CustomException("cannot impersonate yourself", errors: null, System.Net.HttpStatusCode.BadRequest)
            {
                MessageKey = "Identity.CannotImpersonateYourself",
                ResourceSource = typeof(IdentityResources),
            };
        }

        // Prevent nesting: if the caller is already impersonating, require end-impersonation first.
        var callerClaims = _currentUser.GetUserClaims();
        if (callerClaims is not null
            && callerClaims.Any(c => c.Type == ClaimConstants.ActorSubject))
        {
            throw new CustomException(
                "end current impersonation before starting a new one",
                errors: null,
                System.Net.HttpStatusCode.BadRequest)
            {
                MessageKey = "Identity.EndImpersonationFirst",
                ResourceSource = typeof(IdentityResources),
            };
        }

        var targetClaimsResult = await _identityService
            .BuildClaimsForUserAsync(request.TargetUserId, request.TargetTenantId, cancellationToken);

        if (targetClaimsResult is null)
        {
            throw new NotFoundException("target user not found")
            {
                MessageKey = "Identity.TargetUserNotFound",
                ResourceSource = typeof(IdentityResources),
            };
        }

        var (subject, claims) = targetClaimsResult.Value;
        var targetUserName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;

        // Strip the auto-generated jti from BuildClaimsForUserAsync and inject our own, so the persisted
        // ImpersonationGrant row and the issued JWT share the same jti.
        var jti = Guid.NewGuid().ToString("N");
        var impersonationClaims = claims
            // Drop the target's locale: language is a presentation concern, so the operator reads in
            // THEIR own language (falls through to Accept-Language), not the impersonated user's.
            .Where(c => c.Type != JwtRegisteredClaimNames.Jti && c.Type != "locale")
            .Concat(
            [
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                // RFC 8693 actor claims so the issued token carries who is acting.
                new Claim(ClaimConstants.ActorSubject, actorUserId),
                new Claim(ClaimConstants.ActorTenant, actorTenantId)
            ])
            .ToList();

        // Cap the caller-supplied duration server-side (defense in depth: the validator already rejects
        // out-of-range, but a future caller bypassing it must not escape the cap).
        var lifetime = request.DurationMinutes is { } minutes
            ? TimeSpan.FromMinutes(Math.Clamp(minutes, 1, StartImpersonationCommandValidator.MaxImpersonationMinutes))
            : (TimeSpan?)null;

        var startedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var (accessToken, expiresAt) = await _tokenService.IssueAccessOnlyAsync(
            subject, impersonationClaims, lifetime, cancellationToken);

        // Persist the grant AFTER issuance so a failed issue leaves no orphan grant. CreateAsync primes the
        // cache so the JWT validation hook sees status=Active on the next request without a DB hit.
        await _grantService.CreateAsync(new CreateGrantInput(
            Jti: jti,
            ActorUserId: actorUserId,
            ActorUserName: actorUserName,
            ActorTenantId: actorTenantId,
            ImpersonatedUserId: subject,
            ImpersonatedUserName: targetUserName,
            ImpersonatedTenantId: request.TargetTenantId,
            Reason: request.Reason ?? string.Empty,
            StartedAtUtc: startedAtUtc,
            ExpiresAtUtc: expiresAt,
            ClientId: _requestContext.ClientId,
            IpAddress: _requestContext.IpAddress,
            UserAgent: _requestContext.UserAgent), cancellationToken);

        await _securityAudit.ImpersonationStartedAsync(
            actorUserId: actorUserId,
            actorTenantId: actorTenantId,
            targetUserId: subject,
            targetTenantId: request.TargetTenantId,
            clientId: _requestContext.ClientId ?? "unknown",
            ip: _requestContext.IpAddress ?? "unknown",
            userAgent: _requestContext.UserAgent ?? "unknown",
            reason: request.Reason ?? string.Empty,
            ct: cancellationToken);

        _logger.LogWarning(
            "Impersonation started: actor {ActorUserId}@{ActorTenant} -> target {TargetUserId}@{TargetTenant} jti={Jti}",
            actorUserId, actorTenantId, subject, request.TargetTenantId, jti);

        return new ImpersonationResponse(
            AccessToken: accessToken,
            AccessTokenExpiresAt: expiresAt,
            ActorUserId: actorUserId,
            ActorTenantId: actorTenantId,
            ImpersonatedUserId: subject,
            ImpersonatedTenantId: request.TargetTenantId);
    }
}
