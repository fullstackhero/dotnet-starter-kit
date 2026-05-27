using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;

namespace FSH.Modules.Multitenancy.Features.v1.ChangeTenantActivation;

internal sealed class ChangeTenantActivationCommandValidator : AbstractValidator<ChangeTenantActivationCommand>
{
    public ChangeTenantActivationCommandValidator() =>
       RuleFor(t => t.TenantId)
           .NotEmpty();
}