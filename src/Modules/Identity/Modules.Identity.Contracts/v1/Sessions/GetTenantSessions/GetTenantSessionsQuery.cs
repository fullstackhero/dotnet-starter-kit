using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Sessions.GetTenantSessions;

/// <summary>
/// Returns all sessions across the current tenant, paged and optionally
/// filtered. Used by the admin "system sessions" surface — separate from
/// the per-user GetUserSessions query because it needs a different
/// permission and a different shape (paged vs. flat list).
/// </summary>
public sealed record GetTenantSessionsQuery : IQuery<PagedResponse<UserSessionDto>>
{
    /// <summary>When true, includes expired/revoked sessions. Default is active-only.</summary>
    public bool IncludeInactive { get; init; }

    /// <summary>Optional substring filter applied to user name, email, or IP address.</summary>
    public string? Search { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
