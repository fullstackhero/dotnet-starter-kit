using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.Bills.Create.v1;

public sealed class CreateBillCommandValidator : AbstractValidator<CreateBillCommand>
{
    public CreateBillCommandValidator()
    {
        RuleFor(b => b.BillingMonth).InclusiveBetween(1, 12);
        RuleFor(b => b.BillingYear).GreaterThan(2000);
        RuleFor(b => b.TotalAmount).GreaterThanOrEqualTo(0);
        RuleFor(b => b.TotalConsumption).GreaterThanOrEqualTo(0);
        RuleFor(b => b.FixedCharge).GreaterThanOrEqualTo(0);
        RuleFor(b => b.VariableCharge).GreaterThanOrEqualTo(0);
    }
}
