using FluentValidation;
using FSH.Framework.Persistence;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.v1.CreateTenant;

namespace FSH.Modules.Multitenancy.Features.v1.CreateTenant;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator(ITenantService tenantService, IConnectionStringValidator connectionStringValidator)
    {
        RuleFor(t => t.Id).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (id, ct) => !await tenantService.ExistsWithIdAsync(id, ct).ConfigureAwait(false))
            .WithMessage((_, id) => $"Tenant {id} already exists.");

        RuleFor(t => t.Name).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (name, ct) => !await tenantService.ExistsWithNameAsync(name!, ct).ConfigureAwait(false))
            .WithMessage((_, name) => $"Tenant {name} already exists.");

        RuleFor(t => t.ConnectionString).Cascade(CascadeMode.Stop)
            .Must((_, cs) => string.IsNullOrWhiteSpace(cs) || connectionStringValidator.TryValidate(cs))
            .WithMessage("Connection string invalid.");

        RuleFor(t => t.AdminEmail).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();

        // Admin password is now operator-supplied rather than a hardcoded default.
        // The minimum 8-char rule matches the Identity password policy floor; the
        // mixed-character requirements (digit / upper / non-alpha) are enforced
        // later by ASP.NET Identity's PasswordValidators when the seed runs.
        RuleFor(t => t.AdminPassword).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Admin password must be at least 8 characters.");
    }
}