namespace FSH.WebApi.Application.Multitenancy;

public class DeactivateTenantRequest : IRequest<string>
{
    public string TenantKey { get; set; } = default!;

    public DeactivateTenantRequest(string tenantKey) => TenantKey = tenantKey;
}

public class DeactivateTenantRequestValidator : CustomValidator<DeactivateTenantRequest>
{
    public DeactivateTenantRequestValidator() =>
        RuleFor(t => t.TenantKey)
            .NotEmpty();
}

public class DeactivateTenantRequestHandler : IRequestHandler<DeactivateTenantRequest, string>
{
    private readonly ITenantRepository _repository;

    public DeactivateTenantRequestHandler(ITenantRepository repository) => _repository = repository;

    public async Task<string> Handle(DeactivateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetBySpecAsync(new TenantByKeySpec(request.TenantKey), cancellationToken);

        _ = tenant ?? throw new NotFoundException("Tenant not Found.");

        if (!tenant.IsActive)
        {
            throw new ConflictException("Tenant is already Deactivated.");
        }

        tenant.Deactivate();

        await _repository.UpdateAsync(tenant, cancellationToken);

        return $"Tenant {tenant.Key} is now Deactivated.";
    }
}