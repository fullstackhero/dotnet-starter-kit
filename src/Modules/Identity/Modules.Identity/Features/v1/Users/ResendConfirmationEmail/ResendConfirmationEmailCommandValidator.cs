using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Users.ResendConfirmationEmail;

namespace FSH.Modules.Identity.Features.v1.Users.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailCommandValidator : AbstractValidator<ResendConfirmationEmailCommand>
{
    public ResendConfirmationEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
