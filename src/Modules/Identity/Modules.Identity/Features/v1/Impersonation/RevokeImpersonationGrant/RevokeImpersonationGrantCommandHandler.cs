using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Impersonation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.RevokeImpersonationGrant;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Features.v1.Impersonation.RevokeImpersonationGrant;

public sealed class RevokeImpersonationGrantCommandHandler(
    IImpersonationGrantService grantService,
    ICurrentUser currentUser,
    ISecurityAudit securityAudit,
    IRequestContext requestContext,
    ILogger<RevokeImpersonationGrantCommandHandler> logger)
    : ICommandHandler<RevokeImpersonationGrantCommand, ImpersonationGrantDto>
{
    public async ValueTask<ImpersonationGrantDto> Handle(
        RevokeImpersonationGrantCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }

        var callerUserId = currentUser.GetUserId().ToString();
        var callerTenantId = currentUser.GetTenant()
            ?? throw new UnauthorizedException("missing tenant context");
        var isRoot = string.Equals(callerTenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal);

        // Load before revoking so we can enforce visibility: tenant admins can
        // only revoke grants targeting their own tenant. Returning 404 (not
        // 403) for cross-tenant grants avoids confirming existence to callers
        // outside the grant's scope.
        var grant = await grantService.GetByIdAsync(request.GrantId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("impersonation grant not found");

        var withinTenant = string.Equals(grant.ImpersonatedTenantId, callerTenantId, StringComparison.Ordinal)
            || string.Equals(grant.ActorTenantId, callerTenantId, StringComparison.Ordinal);

        if (!isRoot && !withinTenant)
        {
            throw new NotFoundException("impersonation grant not found");
        }

        var updated = await grantService.RevokeAsync(
            id: request.GrantId,
            revokedByUserId: callerUserId,
            revokedByUserName: currentUser.Name,
            reason: request.Reason,
            ct: cancellationToken).ConfigureAwait(false);

        // Surface revoke as a first-class security event so it's queryable
        // alongside the Start/End audit entries. The Reason field on the audit
        // is the revocation reason, not the original impersonation reason.
        await securityAudit.ImpersonationEndedAsync(
            actorUserId: grant.ActorUserId,
            actorTenantId: grant.ActorTenantId,
            targetUserId: grant.ImpersonatedUserId,
            targetTenantId: grant.ImpersonatedTenantId,
            clientId: requestContext.ClientId ?? "unknown",
            ct: cancellationToken).ConfigureAwait(false);

        logger.LogWarning(
            "Impersonation grant revoked: grantId={GrantId} jti={Jti} revokedBy={RevokedBy} reason={Reason}",
            updated.Id, updated.Jti, callerUserId, request.Reason ?? "<none>");

        return updated;
    }
}
