using FluentValidation;
using FSH.Framework.Tenant.Contracts.v1.ActivateTenant;

namespace FSH.Framework.Tenant.Features.v1.ActivateTenant;
public sealed class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
{
    public ActivateTenantCommandValidator() =>
       RuleFor(t => t.TenantId)
           .NotEmpty();
}