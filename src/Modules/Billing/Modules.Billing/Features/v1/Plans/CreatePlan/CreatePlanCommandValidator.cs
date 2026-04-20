using FluentValidation;
using FSH.Modules.Billing.Contracts.v1.Plans;

namespace FSH.Modules.Billing.Features.v1.Plans.CreatePlan;

public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.MonthlyBasePrice).GreaterThanOrEqualTo(0);
    }
}
