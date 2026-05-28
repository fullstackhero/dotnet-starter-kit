using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.v1.AdjustTenantValidity;

namespace FSH.Modules.Multitenancy.Features.v1.AdjustTenantValidity;

public sealed class AdjustTenantValidityCommandValidator : AbstractValidator<AdjustTenantValidityCommand>
{
    public AdjustTenantValidityCommandValidator()
    {
        RuleFor(t => t.TenantId).NotEmpty();

        RuleFor(t => t.ValidUpto)
            .Must(d => d != default)
            .WithMessage("A valid 'validUpto' date is required.");
    }
}
