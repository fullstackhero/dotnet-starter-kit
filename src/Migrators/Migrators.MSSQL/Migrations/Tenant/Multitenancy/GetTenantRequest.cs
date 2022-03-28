namespace FSH.WebApi.Application.Multitenancy;

public class GetTenantRequest : IRequest<TenantDto>
{
    public string TenantId { get; set; } = default!;

    public GetTenantRequest(string tenantId) => TenantId = tenantId;
}

public class GetTenantRequestValidator : CustomValidator<GetTenantRequest>
{
    public GetTenantRequestValidator() =>
        RuleFor(t => t.TenantId)
            .NotEmpty();
}

public class GetTenantRequestHandler : IRequestHandler<GetTenantRequest, TenantDto>
{
    private readonly ITenantService _tenantService;

    public GetTenantRequestHandler(ITenantService tenantService) => _tenantService = tenantService;

    public Task<TenantDto> Handle(GetTenantRequest request, CancellationToken cancellationToken) =>
        _tenantService.GetByIdAsync(request.TenantId);
}