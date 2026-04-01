using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;

namespace FSH.Modules.Identity.Features.v1.Users.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(IdentityValidationMessages.UserIdRequired);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(IdentityValidationMessages.ConfirmationCodeRequired);

        RuleFor(x => x.Tenant)
            .NotEmpty().WithMessage(IdentityValidationMessages.TenantRequired);
    }
}