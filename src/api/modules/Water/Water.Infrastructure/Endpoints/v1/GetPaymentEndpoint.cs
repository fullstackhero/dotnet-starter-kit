using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Payments.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class GetPaymentEndpoint
{
    internal static RouteHandlerBuilder MapGetPaymentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetPaymentRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetPaymentEndpoint))
            .WithSummary("gets payment by id")
            .WithDescription("gets payment by id")
            .Produces<PaymentResponse>()
            .RequirePermission("Permissions.Payments.View")
            .MapToApiVersion(1);
    }
}
