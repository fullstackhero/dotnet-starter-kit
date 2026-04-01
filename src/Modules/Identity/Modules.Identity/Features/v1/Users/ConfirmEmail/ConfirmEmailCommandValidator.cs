using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;

namespace FSH.Modules.Identity.Features.v1.Users.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("User ID"));

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Confirmation code"));

        RuleFor(x => x.Tenant)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Tenant"));
    }
}