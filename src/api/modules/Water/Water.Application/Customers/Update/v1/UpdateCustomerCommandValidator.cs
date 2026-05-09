using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.Customers.Update.v1;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(c => c.FullName).NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(c => c.Address).MaximumLength(500);
        RuleFor(c => c.ContactNumber).MaximumLength(20);
        RuleFor(c => c.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
