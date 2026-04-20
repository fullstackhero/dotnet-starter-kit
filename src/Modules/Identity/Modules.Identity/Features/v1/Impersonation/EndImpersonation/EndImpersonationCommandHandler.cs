using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Impersonation.EndImpersonation;
using Mediator;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace FSH.Modules.Identity.Features.v1.Impersonation.EndImpersonation;

public sealed class EndImpersonationCommandHandler
    : ICommandHandler<EndImpersonationCommand, TokenResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ISecurityAudit _securityAudit;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<EndImpersonationCommandHandler> _logger;

    public EndImpersonationCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ISecurityAudit securityAudit,
        ICurrentUser currentUser,
        IRequestContext requestContext,
        ILogger<EndImpersonationCommandHandler> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _securityAudit = securityAudit;
        _currentUser = currentUser;
        _requestContext = requestContext;
        _logger = logger;
    }

    public async ValueTask<TokenResponse> Handle(
        EndImpersonationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }

        var claims = _currentUser.GetUserClaims()?.ToList()
            ?? throw new UnauthorizedException();

        var actorUserId = claims.FirstOrDefault(c => c.Type == ClaimConstants.ActorSubject)?.Value;
        var actorTenantId = claims.FirstOrDefault(c => c.Type == ClaimConstants.ActorTenant)?.Value;

        if (string.IsNullOrWhiteSpace(actorUserId) || string.IsNullOrWhiteSpace(actorTenantId))
        {
            throw new CustomException("current session is not an impersonation session");
        }

        var impersonatedUserId = _currentUser.GetUserId().ToString();
        var impersonatedTenantId = _currentUser.GetTenant() ?? string.Empty;

        var actorClaimsResult = await _identityService
            .BuildClaimsForUserAsync(actorUserId, actorTenantId, cancellationToken);

        if (actorClaimsResult is null)
        {
            throw new NotFoundException("original actor not found");
        }

        var (subject, actorClaims) = actorClaimsResult.Value;

        var token = await _tokenService.IssueAsync(subject, actorClaims, actorTenantId, cancellationToken);
        await _identityService.StoreRefreshTokenAsync(subject, token.RefreshToken, token.RefreshTokenExpiresAt, cancellationToken);

        await _securityAudit.ImpersonationEndedAsync(
            actorUserId: actorUserId,
            actorTenantId: actorTenantId,
            targetUserId: impersonatedUserId,
            targetTenantId: impersonatedTenantId,
            clientId: _requestContext.ClientId ?? "unknown",
            ct: cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Impersonation ended: actor {ActorUserId}@{ActorTenant} returned from {TargetUserId}@{TargetTenant}",
                actorUserId, actorTenantId, impersonatedUserId, impersonatedTenantId);
        }

        return token;
    }
}
