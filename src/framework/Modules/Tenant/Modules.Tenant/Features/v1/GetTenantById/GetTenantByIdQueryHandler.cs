using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Tenant.Contracts.Dtos;
using FSH.Framework.Tenant.Contracts.v1.GetTenantById;
using FSH.Framework.Tenant.Services;
using Mapster;

namespace FSH.Framework.Tenant.Features.v1.GetTenantById;
internal class GetTenantByIdQueryHandler(ITenantService service)
    : IQueryHandler<GetTenantByIdQuery, GetTenantByIdQueryResponse>
{
    public async Task<GetTenantByIdQueryResponse> HandleAsync(GetTenantByIdQuery query, CancellationToken cancellationToken = default)
    {
        var tenant = await service.GetByIdAsync(query.TenantId);
        var tenantDto = tenant.Adapt<TenantDto>();
        return new GetTenantByIdQueryResponse(tenantDto);
    }
}
