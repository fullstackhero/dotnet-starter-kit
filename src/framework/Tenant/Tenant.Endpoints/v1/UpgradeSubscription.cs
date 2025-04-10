using FluentValidation;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Tenant.Endpoints;

public static class UpgradeSubscription
{
    public record Command(string Tenant, DateTime ExtendedExpiryDate) : ICommand<Response>;
    public record Response(DateTime NewValidity, string Tenant);
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(t => t.Tenant).NotEmpty();
            RuleFor(t => t.ExtendedExpiryDate).GreaterThan(DateTime.UtcNow);
        }
    }


    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/upgrade", (Command command, ICommandDispatcher dispatcher) => dispatcher.SendAsync<Command, Response>(command))
                                .WithName(nameof(UpgradeSubscription))
                                .WithSummary("upgrade tenant subscription")
                                .RequirePermission("Permissions.Tenants.Update")
                                .WithDescription("upgrade tenant subscription");
    }
    public class Handler(ITenantService service) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command request, CancellationToken cancellationToken = default)
        {
            var validUpto = await service.UpgradeSubscription(request.Tenant, request.ExtendedExpiryDate);
            return new Response(validUpto, request.Tenant);
        }
    }

}
