using FSH.Framework.Core.MultiTenancy.Abstractions;
using MediatR;

namespace FSH.Framework.Core.MultiTenancy.Features.GetList;
public sealed class GetTenantListHandler(ITenantService tenantService) : IRequestHandler<GetTenantListRquest, List<TenantDto>>
{
    public Task<List<TenantDto>> Handle(GetTenantListRquest request, CancellationToken cancellationToken)
    {
        return tenantService.GetAllAsync();
    }
}
