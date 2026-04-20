using FluentValidation;
using FSH.Modules.Billing.Contracts.v1.Subscriptions;

namespace FSH.Modules.Billing.Features.v1.Subscriptions.AssignSubscription;

public sealed class AssignSubscriptionCommandValidator : AbstractValidator<AssignSubscriptionCommand>
{
    public AssignSubscriptionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PlanKey).NotEmpty().MaximumLength(64);
    }
}
