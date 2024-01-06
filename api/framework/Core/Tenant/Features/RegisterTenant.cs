using FluentValidation;
using FSH.Framework.Core.Abstraction.Persistence;
using FSH.Framework.Core.Tenant.Abstractions;
using MediatR;

namespace FSH.Framework.Core.Tenant.Features;
public static class RegisterTenant
{
    public sealed record Command(string Id,
        string Name,
        string? ConnectionString,
        string AdminEmail,
        string? Issuer) : IRequest<string>;

    public sealed class Handler(ITenantService service) : IRequestHandler<Command, string>
    {
        public Task<string> Handle(Command request, CancellationToken cancellationToken)
        {
            return service.CreateAsync(request, cancellationToken);
        }
    }
    public class Validator : AbstractValidator<Command>
    {
        public Validator(
            ITenantService tenantService,
            IConnectionStringValidator connectionStringValidator)
        {
            RuleFor(t => t.Id).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (id, _) => !await tenantService.ExistsWithIdAsync(id).ConfigureAwait(false))
                    .WithMessage((_, id) => $"Tenant {id} already exists.");

            RuleFor(t => t.Name).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (name, _) => !await tenantService.ExistsWithNameAsync(name!).ConfigureAwait(false))
                    .WithMessage((_, name) => $"Tenant {name} already exists.");

            RuleFor(t => t.ConnectionString).Cascade(CascadeMode.Stop)
                .Must((_, cs) => string.IsNullOrWhiteSpace(cs) || connectionStringValidator.TryValidate(cs))
                    .WithMessage("Connection string invalid.");

            RuleFor(t => t.AdminEmail).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
