using FluentValidation;
using FSH.Framework.Tenant.Contracts.v1.UpgradeTenant;

namespace FSH.Framework.Tenant.Features.v1.UpgradeTenant;
public sealed class UpgradeTenantCommandValidator : AbstractValidator<UpgradeTenantCommand>
{
    public UpgradeTenantCommandValidator()
    {
        RuleFor(t => t.Tenant).NotEmpty();
        RuleFor(t => t.ExtendedExpiryDate).GreaterThan(DateTime.UtcNow);
    }
}