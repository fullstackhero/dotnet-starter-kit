using FluentValidation;

namespace FSH.Framework.Core.Tenant.Features.DisableTenant;
internal class DisableTenantValidator : AbstractValidator<DisableTenantCommand>
{
    public DisableTenantValidator() =>
       RuleFor(t => t.TenantId)
           .NotEmpty();
}
