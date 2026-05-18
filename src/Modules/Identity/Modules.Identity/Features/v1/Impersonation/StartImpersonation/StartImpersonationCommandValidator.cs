using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;

namespace FSH.Modules.Identity.Features.v1.Impersonation.StartImpersonation;

public sealed class StartImpersonationCommandValidator : AbstractValidator<StartImpersonationCommand>
{
    /// <summary>
    /// Upper bound on impersonation token lifetime — the server will silently
    /// cap to this even if the validator passes, but we reject obvious abuse
    /// (negative, zero, or absurd values) up front.
    /// </summary>
    public const int MaxImpersonationMinutes = 60;

    public StartImpersonationCommandValidator()
    {
        RuleFor(p => p.TargetUserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(p => p.TargetTenantId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(p => p.DurationMinutes!.Value)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxImpersonationMinutes)
            .WithMessage($"Duration must be between 1 and {MaxImpersonationMinutes} minutes.")
            .When(p => p.DurationMinutes.HasValue);
    }
}
