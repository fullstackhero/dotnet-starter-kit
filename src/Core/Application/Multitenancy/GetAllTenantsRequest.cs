using Mapster;

namespace FSH.WebApi.Application.Multitenancy;

public class GetAllTenantsRequest : IRequest<List<TenantDto>>
{
}

public class GetAllTenantsRequestHandler : IRequestHandler<GetAllTenantsRequest, List<TenantDto>>
{
    private readonly ITenantReadRepository _repository;
    private readonly IMakeSecureConnectionString _securer;

    public GetAllTenantsRequestHandler(ITenantReadRepository repository, IMakeSecureConnectionString securer) =>
        (_repository, _securer) = (repository, securer);

    public async Task<List<TenantDto>> Handle(GetAllTenantsRequest request, CancellationToken cancellationToken)
    {
        var tenants = await _repository.ListAsync(cancellationToken);

        var tenantList = tenants.Adapt<List<TenantDto>>();

        tenantList.ForEach(t => t.ConnectionString = _securer.MakeSecure(t.ConnectionString));

        return tenantList;
    }
}