using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;

namespace FSH.Modules.Identity.Features.v1.Impersonation.StartImpersonation;

public sealed class StartImpersonationCommandValidator : AbstractValidator<StartImpersonationCommand>
{
    public StartImpersonationCommandValidator()
    {
        RuleFor(p => p.TargetUserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(p => p.TargetTenantId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
    }
}
