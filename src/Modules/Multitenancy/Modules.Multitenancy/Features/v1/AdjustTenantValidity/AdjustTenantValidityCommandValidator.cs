using FluentValidation;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.v1.AdjustTenantValidity;
using FSH.Modules.Multitenancy.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Multitenancy.Features.v1.AdjustTenantValidity;

public sealed class AdjustTenantValidityCommandValidator : AbstractValidator<AdjustTenantValidityCommand>
{
    public AdjustTenantValidityCommandValidator(IStringLocalizer<MultitenancyResources> localizer)
    {
        RuleFor(t => t.TenantId).NotEmpty();

        // The root operator tenant must never expire — block adjusting its validity (mirrors the
        // Activate/Deactivate guards that already refuse the root tenant).
        RuleFor(t => t.TenantId)
            .Must(id => !string.Equals(id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
            .WithMessage(_ => localizer["Validation.RootTenantValidityImmutable"]);

        RuleFor(t => t.ValidUpto)
            .Must(d => d != default)
            .WithMessage(_ => localizer["Validation.ValidUptoRequired"]);
    }
}
