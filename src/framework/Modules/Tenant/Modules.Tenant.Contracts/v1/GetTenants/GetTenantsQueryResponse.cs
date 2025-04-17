using FSH.Framework.Tenant.Contracts.Dtos;

namespace FSH.Framework.Tenant.Contracts.v1.GetTenants;
public sealed record GetTenantsQueryResponse(IReadOnlyCollection<TenantDto> Tenants);