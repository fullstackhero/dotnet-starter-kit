namespace FSH.WebApi.Application.Multitenancy;

public class UpgradeSubscriptionRequest : IRequest<string>
{
    public string TenantId { get; set; } = default!;
    public DateTime ExtendedExpiryDate { get; set; }
}

public class UpgradeSubscriptionRequestValidator : CustomValidator<UpgradeSubscriptionRequest>
{
    public UpgradeSubscriptionRequestValidator() =>
        RuleFor(t => t.TenantId)
            .NotEmpty();
}

public class UpgradeSubscriptionRequestHandler : IRequestHandler<UpgradeSubscriptionRequest, string>
{
    private readonly ITenantService _tenantService;

    public UpgradeSubscriptionRequestHandler(ITenantService tenantService) => _tenantService = tenantService;

    public Task<string> Handle(UpgradeSubscriptionRequest request, CancellationToken cancellationToken) =>
        _tenantService.UpdateSubscription(request.TenantId, request.ExtendedExpiryDate);
}