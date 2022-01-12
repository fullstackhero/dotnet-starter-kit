namespace FSH.WebApi.Application.Multitenancy;

public class UpgradeSubscriptionRequest : IRequest<string>
{
    public string TenantKey { get; set; } = default!;
    public DateTime ExtendedExpiryDate { get; set; }
}

public class UpgradeSubscriptionRequestValidator : CustomValidator<UpgradeSubscriptionRequest>
{
    public UpgradeSubscriptionRequestValidator() =>
        RuleFor(t => t.TenantKey)
            .NotEmpty();
}

public class UpgradeSubscriptionRequestHandler : IRequestHandler<UpgradeSubscriptionRequest, string>
{
    private readonly ITenantRepository _repository;

    public UpgradeSubscriptionRequestHandler(ITenantRepository repository) => _repository = repository;

    public async Task<string> Handle(UpgradeSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetBySpecAsync(new TenantByKeySpec(request.TenantKey), cancellationToken);

        _ = tenant ?? throw new NotFoundException("Tenant Not Found.");

        tenant.SetValidity(request.ExtendedExpiryDate);

        await _repository.UpdateAsync(tenant, cancellationToken);

        return $"Tenant {request.TenantKey}'s Subscription Upgraded. Now Valid till {tenant.ValidUpto}.";
    }
}