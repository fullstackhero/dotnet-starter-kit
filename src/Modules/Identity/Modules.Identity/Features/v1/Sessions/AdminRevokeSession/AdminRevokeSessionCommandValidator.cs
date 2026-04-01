using FluentValidation;
using FSH.Modules.Identity.Constants;
using FSH.Modules.Identity.Contracts.v1.Sessions.AdminRevokeSession;

namespace FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeSession;

public sealed class AdminRevokeSessionCommandValidator : AbstractValidator<AdminRevokeSessionCommand>
{
    public AdminRevokeSessionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(IdentityValidationMessages.UserIdRequired);

        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage(IdentityValidationMessages.SessionIdRequired);

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage(IdentityValidationMessages.ReasonMaxLength)
            .When(x => x.Reason is not null);
    }
}