using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Tenant.Contracts.Dtos;
using FSH.Framework.Tenant.Contracts.v1.GetTenants;
using FSH.Framework.Tenant.Services;
using Mapster;

namespace FSH.Framework.Tenant.Features.v1.GetTenants;

public sealed class GetTenantsQueryHandler(ITenantService service)
    : IQueryHandler<GetTenantsQuery, GetTenantsQueryResponse>
{
    public async Task<GetTenantsQueryResponse> HandleAsync(GetTenantsQuery query, CancellationToken cancellationToken = default)
    {
        var tenants = await service.GetAllAsync();
        return new GetTenantsQueryResponse(tenants.Adapt<List<TenantDto>>());
    }
}