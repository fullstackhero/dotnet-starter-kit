using FluentValidation;

namespace FSH.Framework.Core.Tenant.Features.ActivateTenant;
internal class ActivateTenantValidator : AbstractValidator<ActivateTenantCommand>
{
    public ActivateTenantValidator() =>
       RuleFor(t => t.TenantId)
           .NotEmpty();
}
