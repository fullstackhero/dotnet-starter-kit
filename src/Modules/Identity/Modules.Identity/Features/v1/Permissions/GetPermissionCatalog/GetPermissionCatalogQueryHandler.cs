using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Permissions.GetPermissionCatalog;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Permissions.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler(
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : IQueryHandler<GetPermissionCatalogQuery, IReadOnlyList<PermissionCatalogEntryDto>>
{
    public ValueTask<IReadOnlyList<PermissionCatalogEntryDto>> Handle(
        GetPermissionCatalogQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId = tenantAccessor.MultiTenantContext.TenantInfo?.Id;
        bool isRoot = string.Equals(tenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal);

        // Matches the same root-vs-admin rule used by RolePermissionSyncer so the catalog the
        // SPA edits agrees with the set the syncer would push into a tenant's role claims.
        var source = isRoot
            ? PermissionConstants.Admin.Concat(PermissionConstants.Root).DistinctBy(p => p.Name)
            : PermissionConstants.Admin;

        IReadOnlyList<PermissionCatalogEntryDto> result =
        [
            .. source.Select(p => new PermissionCatalogEntryDto(
                p.Name,
                p.Description,
                p.Resource,
                p.Action,
                p.IsBasic,
                p.IsRoot))
        ];

        return ValueTask.FromResult(result);
    }
}
