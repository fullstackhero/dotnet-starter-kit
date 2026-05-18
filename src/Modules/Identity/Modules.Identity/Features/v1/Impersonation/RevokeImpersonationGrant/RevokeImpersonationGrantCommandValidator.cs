using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.RevokeImpersonationGrant;

namespace FSH.Modules.Identity.Features.v1.Impersonation.RevokeImpersonationGrant;

public sealed class RevokeImpersonationGrantCommandValidator : AbstractValidator<RevokeImpersonationGrantCommand>
{
    public RevokeImpersonationGrantCommandValidator()
    {
        RuleFor(p => p.GrantId)
            .NotEmpty();

        RuleFor(p => p.Reason)
            .MaximumLength(512)
            .When(p => !string.IsNullOrEmpty(p.Reason));
    }
}
