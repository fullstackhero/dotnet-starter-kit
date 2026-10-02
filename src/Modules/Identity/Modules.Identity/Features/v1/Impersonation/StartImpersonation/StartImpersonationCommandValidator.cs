using FluentValidation;
using FSH.Framework.Core.Localization;
using FSH.Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Identity.Features.v1.Impersonation.StartImpersonation;

public sealed class StartImpersonationCommandValidator : AbstractValidator<StartImpersonationCommand>
{
    /// <summary>
    /// Upper bound on impersonation token lifetime — the server will silently
    /// cap to this even if the validator passes, but we reject obvious abuse
    /// (negative, zero, or absurd values) up front.
    /// </summary>
    public const int MaxImpersonationMinutes = 60;

    public StartImpersonationCommandValidator(IStringLocalizer<SharedResources> localizer)
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
            .WithMessage(_ => localizer["Validation.ImpersonationDurationRange", MaxImpersonationMinutes])
            .When(p => p.DurationMinutes.HasValue);
    }
}
