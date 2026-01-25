using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;

namespace FSH.Modules.Identity.Features.v1.Users.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Confirmation code is required.");

        RuleFor(x => x.Tenant)
            .NotEmpty().WithMessage("Tenant is required.");
    }
}
