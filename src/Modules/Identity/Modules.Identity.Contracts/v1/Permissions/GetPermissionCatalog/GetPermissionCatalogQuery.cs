using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Permissions.GetPermissionCatalog;

/// <summary>
/// Returns every permission registered with the host's <c>PermissionConstants</c> registry,
/// filtered to the caller's tenant context: non-root tenants get the Admin set; the root
/// tenant additionally gets the platform Root set. Mirrors the rule applied by
/// <c>RolePermissionSyncer</c> so the editor catalogue and the syncable target stay in lockstep.
/// </summary>
public sealed record GetPermissionCatalogQuery() : IQuery<IReadOnlyList<PermissionCatalogEntryDto>>;
