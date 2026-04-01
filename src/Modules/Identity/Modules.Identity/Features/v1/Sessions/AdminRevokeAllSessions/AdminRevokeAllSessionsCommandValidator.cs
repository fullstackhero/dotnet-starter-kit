using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Sessions.AdminRevokeAllSessions;

namespace FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;

public sealed class AdminRevokeAllSessionsCommandValidator : AbstractValidator<AdminRevokeAllSessionsCommand>
{
    public AdminRevokeAllSessionsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("User ID"));

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage(IdentityValidationMessages.MaxLength("Reason", 500))
            .When(x => x.Reason is not null);
    }
}