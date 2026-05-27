using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.v1.UpgradeTenant;

namespace FSH.Modules.Multitenancy.Features.v1.UpgradeTenant;

public sealed class UpgradeTenantCommandValidator : AbstractValidator<UpgradeTenantCommand>
{
    public UpgradeTenantCommandValidator()
    {
        RuleFor(t => t.Tenant).NotEmpty();
        RuleFor(t => t.ExtendedExpiryDate).GreaterThan(TimeProvider.System.GetUtcNow().UtcDateTime);
    }
}