using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Impersonation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.GetImpersonationGrants;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Impersonation.GetImpersonationGrants;

public sealed class GetImpersonationGrantsQueryHandler(
    IImpersonationGrantService grantService,
    ICurrentUser currentUser)
    : IQueryHandler<GetImpersonationGrantsQuery, IReadOnlyList<ImpersonationGrantDto>>
{
    public async ValueTask<IReadOnlyList<ImpersonationGrantDto>> Handle(
        GetImpersonationGrantsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var callerTenant = currentUser.GetTenant()
            ?? throw new UnauthorizedException("missing tenant context")
            {
                MessageKey = "Error.InvalidTenant",
            };
        var isRoot = string.Equals(callerTenant, MultitenancyConstants.Root.Id, StringComparison.Ordinal);

        // Tenant scoping: root operators target any tenant; tenant admins are locked to their
        // own regardless of input. Mirrors the StartImpersonation cross-tenant rule.
        var tenantFilter = isRoot ? request.ImpersonatedTenantId : callerTenant;

        return await grantService.ListAsync(
            status: request.Status,
            impersonatedTenantId: tenantFilter,
            actorUserId: request.ActorUserId,
            take: request.Take,
            ct: cancellationToken).ConfigureAwait(false);
    }
}
