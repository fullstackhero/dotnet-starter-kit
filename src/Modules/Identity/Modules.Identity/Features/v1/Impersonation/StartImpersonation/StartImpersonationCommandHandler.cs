using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Impersonation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;
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
            ?? throw new UnauthorizedException("missing tenant context");
        var actorUserName = _currentUser.Name;

        // Cross-tenant impersonation requires the actor to be in the root tenant. Tenant admins
        // can only impersonate users within their own tenant.
        if (!string.Equals(actorTenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal)
            && !string.Equals(actorTenantId, request.TargetTenantId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("cross-tenant impersonation is restricted to platform operators");
        }

        // Prevent self-impersonation (nothing to gain, confuses audit trail). This
        // is a caller error → must be a 4xx, not the default 500 that CustomException
        // assumes.
        if (string.Equals(actorUserId, request.TargetUserId, StringComparison.Ordinal)
            && string.Equals(actorTenantId, request.TargetTenantId, StringComparison.Ordinal))
        {
            throw new CustomException("cannot impersonate yourself", errors: null, System.Net.HttpStatusCode.BadRequest);
        }

        // Prevent nesting: if the caller is already impersonating, require end-impersonation first.
        var callerClaims = _currentUser.GetUserClaims();
        if (callerClaims is not null
            && callerClaims.Any(c => c.Type == ClaimConstants.ActorSubject))
        {
            throw new CustomException(
                "end current impersonation before starting a new one",
                errors: null,
                System.Net.HttpStatusCode.BadRequest);
        }

        var targetClaimsResult = await _identityService
            .BuildClaimsForUserAsync(request.TargetUserId, request.TargetTenantId, cancellationToken);

        if (targetClaimsResult is null)
        {
            throw new NotFoundException("target user not found");
        }

        var (subject, claims) = targetClaimsResult.Value;
        var targetUserName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;

        // We need to know the jti of the issued token so we can persist a
        // matching ImpersonationGrant. BuildClaimsForUserAsync hands us claims
        // that already include an auto-generated jti — strip it and inject our
        // own deterministic value so the grant row + JWT agree on it.
        var jti = Guid.NewGuid().ToString("N");
        var impersonationClaims = claims
            .Where(c => c.Type != JwtRegisteredClaimNames.Jti)
            .Concat(
            [
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                // RFC 8693 actor claims so the issued token carries who is acting.
                new Claim(ClaimConstants.ActorSubject, actorUserId),
                new Claim(ClaimConstants.ActorTenant, actorTenantId)
            ])
            .ToList();

        // Caller-supplied duration is capped server-side (defense in depth — the
        // validator already rejects out-of-range, but a future caller bypassing
        // validation shouldn't escape the cap).
        var lifetime = request.DurationMinutes is { } minutes
            ? TimeSpan.FromMinutes(Math.Clamp(minutes, 1, StartImpersonationCommandValidator.MaxImpersonationMinutes))
            : (TimeSpan?)null;

        var startedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var (accessToken, expiresAt) = await _tokenService.IssueAccessOnlyAsync(
            subject, impersonationClaims, lifetime, cancellationToken);

        // Persist the grant AFTER the token issues — if issuance fails we don't
        // leave an orphan grant row claiming to track a session that never was.
        // Cache priming inside CreateAsync ensures the JWT validation hook sees
        // status=Active on the very next request without a DB hit.
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
