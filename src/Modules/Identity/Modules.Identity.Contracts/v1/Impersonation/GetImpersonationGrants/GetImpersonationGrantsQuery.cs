using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Impersonation.GetImpersonationGrants;

// Status: filter to Active, Ended, Revoked, or Expired. Null returns all.
// ImpersonatedTenantId: filter to grants targeting users in this tenant.
//   Tenant admins are forced to their own tenant server-side regardless.
// ActorUserId: filter to grants started by this actor user id.
public sealed record GetImpersonationGrantsQuery(
    ImpersonationGrantStatus? Status = null,
    string? ImpersonatedTenantId = null,
    string? ActorUserId = null,
    int Take = 100)
    : IQuery<IReadOnlyList<ImpersonationGrantDto>>;
