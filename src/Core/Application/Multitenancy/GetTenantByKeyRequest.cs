using Mapster;

namespace FSH.WebApi.Application.Multitenancy;

public class GetTenantByKeyRequest : IRequest<TenantDto>
{
    public string Key { get; set; } = default!;

    public GetTenantByKeyRequest(string key) => Key = key;
}

public class GetTenantByKeyRequestHandler : IRequestHandler<GetTenantByKeyRequest, TenantDto>
{
    private readonly ITenantReadRepository _repository;
    private readonly IStringLocalizer<GetTenantByKeyRequestHandler> _localizer;

    public GetTenantByKeyRequestHandler(ITenantReadRepository repository, IStringLocalizer<GetTenantByKeyRequestHandler> localizer) =>
        (_repository, _localizer) = (repository, localizer);

    public async Task<TenantDto> Handle(GetTenantByKeyRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetBySpecAsync(new TenantByKeySpec(request.Key), cancellationToken);

        _ = tenant ?? throw new NotFoundException(string.Format(_localizer["entity.notfound"], typeof(Tenant).Name, request.Key));

        return tenant.Adapt<TenantDto>();
    }
}