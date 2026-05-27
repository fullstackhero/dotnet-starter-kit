using FSH.Framework.Shared.Persistence;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.GetTenants;

public sealed class GetTenantsQuery : IPagedQuery, IQuery<PagedResponse<TenantDto>>
{
    public int? PageNumber { get; set; }

    public int? PageSize { get; set; }

    public string? Sort { get; set; }
}