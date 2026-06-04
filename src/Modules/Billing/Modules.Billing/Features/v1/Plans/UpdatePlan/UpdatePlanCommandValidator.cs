using FluentValidation;
using FSH.Modules.Billing.Contracts.v1.Plans;

namespace FSH.Modules.Billing.Features.v1.Plans.UpdatePlan;

public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.MonthlyBasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Interval).IsInEnum();
        RuleFor(x => x.AnnualPrice).GreaterThanOrEqualTo(0).When(x => x.AnnualPrice.HasValue);
    }
}
