using FluentValidation;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Endpoints.v1;
public static class DisableTenant
{
    public sealed record Command(string TenantId) : ICommand<Response>;
    public sealed record Response(string Status);
    public sealed class DisableTenantValidator : AbstractValidator<Command>
    {
        public DisableTenantValidator() =>
           RuleFor(t => t.TenantId)
               .NotEmpty();
    }
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id}/deactivate", (ICommandDispatcher dispatcher, string id)
            => dispatcher.SendAsync<Command, Response>(new Command(id)))
                                .WithName(nameof(DisableTenant))
                                .WithSummary("activate tenant")
                                .RequirePermission("Permissions.Tenants.Update")
                                .WithDescription("activate tenant");
    }
    public sealed class Handler(ITenantService service) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command request, CancellationToken cancellationToken = default)
        {
            var status = await service.DeactivateAsync(request.TenantId);
            return new Response(status);
        }
    }

}
