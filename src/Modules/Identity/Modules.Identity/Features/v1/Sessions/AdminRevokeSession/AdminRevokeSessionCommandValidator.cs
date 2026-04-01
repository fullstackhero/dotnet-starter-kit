using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Sessions.AdminRevokeSession;

namespace FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeSession;

public sealed class AdminRevokeSessionCommandValidator : AbstractValidator<AdminRevokeSessionCommand>
{
    public AdminRevokeSessionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("User ID"));

        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage(IdentityValidationMessages.Required("Session ID"));

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage(IdentityValidationMessages.MaxLength("Reason", 500))
            .When(x => x.Reason is not null);
    }
}