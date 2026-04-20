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
    private readonly ILogger<StartImpersonationCommandHandler> _logger;

    public StartImpersonationCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ISecurityAudit securityAudit,
        ICurrentUser currentUser,
        IRequestContext requestContext,
        ILogger<StartImpersonationCommandHandler> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _securityAudit = securityAudit;
        _currentUser = currentUser;
        _requestContext = requestContext;
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

        // Cross-tenant impersonation requires the actor to be in the root tenant. Tenant admins
        // can only impersonate users within their own tenant.
        if (!string.Equals(actorTenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal)
            && !string.Equals(actorTenantId, request.TargetTenantId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("cross-tenant impersonation is restricted to platform operators");
        }

        // Prevent self-impersonation (nothing to gain, confuses audit trail)
        if (string.Equals(actorUserId, request.TargetUserId, StringComparison.Ordinal)
            && string.Equals(actorTenantId, request.TargetTenantId, StringComparison.Ordinal))
        {
            throw new CustomException("cannot impersonate yourself");
        }

        // Prevent nesting: if the caller is already impersonating, require end-impersonation first.
        var callerClaims = _currentUser.GetUserClaims();
        if (callerClaims is not null
            && callerClaims.Any(c => c.Type == ClaimConstants.ActorSubject))
        {
            throw new CustomException("end current impersonation before starting a new one");
        }

        var targetClaimsResult = await _identityService
            .BuildClaimsForUserAsync(request.TargetUserId, request.TargetTenantId, cancellationToken);

        if (targetClaimsResult is null)
        {
            throw new NotFoundException("target user not found");
        }

        var (subject, claims) = targetClaimsResult.Value;

        // Inject actor claims per RFC 8693 semantics so the issued token carries who is acting.
        var impersonationClaims = claims.Concat(
        [
            new Claim(ClaimConstants.ActorSubject, actorUserId),
            new Claim(ClaimConstants.ActorTenant, actorTenantId)
        ]).ToList();

        var (accessToken, expiresAt) = await _tokenService.IssueAccessOnlyAsync(
            subject, impersonationClaims, cancellationToken);

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
            "Impersonation started: actor {ActorUserId}@{ActorTenant} -> target {TargetUserId}@{TargetTenant}",
            actorUserId, actorTenantId, subject, request.TargetTenantId);

        return new ImpersonationResponse(
            AccessToken: accessToken,
            AccessTokenExpiresAt: expiresAt,
            ActorUserId: actorUserId,
            ActorTenantId: actorTenantId,
            ImpersonatedUserId: subject,
            ImpersonatedTenantId: request.TargetTenantId);
    }
}
