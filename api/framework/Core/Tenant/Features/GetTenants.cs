using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Dtos;
using MediatR;

namespace FSH.Framework.Core.Tenant.Features;
public static class GetTenants
{
    public sealed class Query : IRequest<List<TenantDetail>>;

    public sealed class Handler(ITenantService service) : IRequestHandler<Query, List<TenantDetail>>
    {
        public Task<List<TenantDetail>> Handle(Query request, CancellationToken cancellationToken)
        {
            return service.GetAllAsync();
        }
    }
}
