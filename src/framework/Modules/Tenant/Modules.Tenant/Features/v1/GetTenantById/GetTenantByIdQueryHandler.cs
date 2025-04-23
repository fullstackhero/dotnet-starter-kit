using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Tenant.Contracts.Dtos;
using FSH.Framework.Tenant.Contracts.v1.GetTenantById;
using FSH.Framework.Tenant.Services;
using Mapster;

namespace FSH.Framework.Tenant.Features.v1.GetTenantById;
public sealed class GetTenantByIdQueryHandler(ITenantService service)
    : IQueryHandler<GetTenantByIdQuery, TenantDto>
{
    public async Task<TenantDto> HandleAsync(GetTenantByIdQuery query, CancellationToken cancellationToken = default)
    {
        var tenant = await service.GetByIdAsync(query.TenantId);
        return tenant.Adapt<TenantDto>();
    }
}