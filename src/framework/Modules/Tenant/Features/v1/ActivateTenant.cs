using FluentValidation;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Features.v1;
public static class ActivateTenant
{
    public sealed record Command(string TenantId) : ICommand<Response>;
    public record Response(string Status);
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() =>
           RuleFor(t => t.TenantId)
               .NotEmpty();
    }
    public sealed class Handler(ITenantService tenantService) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var result = await tenantService.ActivateAsync(command.TenantId, cancellationToken);
            return new Response(result);
        }
    }
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id}/activate", async (ICommandDispatcher dispatcher, string id)
            => await dispatcher.SendAsync(new Command(id)))
                                .WithName(nameof(ActivateTenant))
                                .WithSummary("activate tenant")
                                .RequirePermission("Permissions.Tenants.Update")
                                .WithDescription("activate tenant");
    }
}
