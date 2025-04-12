using FluentValidation;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Features.v1;
public static class CreateTenant
{
    public sealed record Command(string Id, string Name, string? ConnectionString, string AdminEmail, string? Issuer) : ICommand<Response>;
    public sealed record Response(string Id);
    public class Validator : AbstractValidator<Command>
    {
        public Validator(ITenantService tenantService, IConnectionStringValidator connectionStringValidator)
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
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (ICommandDispatcher dispatcher, Command command)
            => await dispatcher.SendAsync(command))
                                .WithName(nameof(ActivateTenant))
                                .WithSummary("activate tenant")
                                .RequirePermission("Permissions.Tenants.Create")
                                .WithDescription("activate tenant");
    }
    public sealed class Handler(ITenantService service) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var tenantId = await service.CreateAsync(command.Id,
                command.Name,
                command.ConnectionString,
                command.AdminEmail,
                command.Issuer,
                cancellationToken);
            return new Response(tenantId);
        }
    }

}
