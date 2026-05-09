using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Payments.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class CreatePaymentEndpoint
{
    internal static RouteHandlerBuilder MapPaymentCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", async (CreatePaymentCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreatePaymentEndpoint))
            .WithSummary("creates a payment")
            .WithDescription("creates a payment")
            .Produces<CreatePaymentResponse>()
            .RequirePermission("Permissions.Payments.Create")
            .MapToApiVersion(1);
    }
}
