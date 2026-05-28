using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.v1.RenewTenant;

namespace FSH.Modules.Multitenancy.Features.v1.RenewTenant;

public sealed class RenewTenantCommandValidator : AbstractValidator<RenewTenantCommand>
{
    public RenewTenantCommandValidator()
    {
        RuleFor(t => t.TenantId).NotEmpty();

        RuleFor(t => t.PlanKey)
            .Matches("^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$")
            .When(t => !string.IsNullOrWhiteSpace(t.PlanKey))
            .WithMessage("Plan key must be a lowercase slug (a-z, 0-9, hyphen).");
    }
}
