using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.v1.RenewTenant;
using FSH.Modules.Multitenancy.Localization;
using Microsoft.Extensions.Localization;

namespace FSH.Modules.Multitenancy.Features.v1.RenewTenant;

public sealed class RenewTenantCommandValidator : AbstractValidator<RenewTenantCommand>
{
    public RenewTenantCommandValidator(IStringLocalizer<MultitenancyResources> localizer)
    {
        RuleFor(t => t.TenantId).NotEmpty();

        RuleFor(t => t.PlanKey)
            .Matches("^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$")
            .When(t => !string.IsNullOrWhiteSpace(t.PlanKey))
            .WithMessage(_ => localizer["Validation.PlanKeySlug"]);
    }
}
