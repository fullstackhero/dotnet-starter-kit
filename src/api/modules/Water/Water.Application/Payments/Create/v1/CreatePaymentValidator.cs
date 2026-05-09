using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.Payments.Create.v1;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(p => p.AmountPaid).GreaterThan(0);
        RuleFor(p => p.TransactionReference).MaximumLength(100);
    }
}
