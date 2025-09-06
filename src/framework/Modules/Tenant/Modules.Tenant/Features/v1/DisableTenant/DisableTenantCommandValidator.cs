using FluentValidation;
using FSH.Framework.Tenant.Contracts.v1.DisableTenant;

namespace FSH.Framework.Tenant.Features.v1.DisableTenant;
internal sealed class DisableTenantCommandValidator : AbstractValidator<DisableTenantCommand>
{
    public DisableTenantCommandValidator() =>
       RuleFor(t => t.TenantId)
           .NotEmpty();
}