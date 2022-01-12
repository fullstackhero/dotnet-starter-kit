namespace FSH.WebApi.Application.Multitenancy;

public class ActivateTenantRequest : IRequest<string>
{
    public string TenantKey { get; set; } = default!;

    public ActivateTenantRequest(string tenantKey) => TenantKey = tenantKey;
}

public class ActivateTenantRequestValidator : CustomValidator<ActivateTenantRequest>
{
    public ActivateTenantRequestValidator() =>
        RuleFor(t => t.TenantKey)
            .NotEmpty();
}

public class ActivateTenantRequestHandler : IRequestHandler<ActivateTenantRequest, string>
{
    private readonly ITenantRepository _repository;

    public ActivateTenantRequestHandler(ITenantRepository repository) => _repository = repository;

    public async Task<string> Handle(ActivateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetBySpecAsync(new TenantByKeySpec(request.TenantKey), cancellationToken);

        _ = tenant ?? throw new NotFoundException("Tenant not Found.");

        if (tenant.IsActive)
        {
            throw new ConflictException("Tenant is already Activated.");
        }

        tenant.Activate();

        await _repository.UpdateAsync(tenant, cancellationToken);

        return $"Tenant {tenant.Key} is now Activated.";
    }
}