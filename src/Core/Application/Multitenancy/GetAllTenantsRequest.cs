namespace FL_CRMS_ERP_WEBAPI.Application.Multitenancy;

public class GetAllTenantsRequest : IRequest<List<TenantDto>>
{
}

public class GetAllTenantsRequestHandler : IRequestHandler<GetAllTenantsRequest, List<TenantDto>>
{
    private readonly ITenantService _tenantService;

    public GetAllTenantsRequestHandler(ITenantService tenantService) => _tenantService = tenantService;

    public Task<List<TenantDto>> Handle(GetAllTenantsRequest request, CancellationToken cancellationToken) =>
        _tenantService.GetAllAsync();
}