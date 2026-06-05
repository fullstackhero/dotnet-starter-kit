using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Impersonation.GetImpersonationGrants;

// Status (null = all) and ActorUserId filters. ImpersonatedTenantId filters by target tenant, but
// tenant admins are forced to their own tenant server-side regardless.
public sealed record GetImpersonationGrantsQuery(
    ImpersonationGrantStatus? Status = null,
    string? ImpersonatedTenantId = null,
    string? ActorUserId = null,
    int Take = 100)
    : IQuery<IReadOnlyList<ImpersonationGrantDto>>;
