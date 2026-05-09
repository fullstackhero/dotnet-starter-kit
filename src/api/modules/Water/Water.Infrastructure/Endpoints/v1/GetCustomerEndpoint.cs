using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Customers.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class GetCustomerEndpoint
{
    internal static RouteHandlerBuilder MapGetCustomerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetCustomerRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetCustomerEndpoint))
            .WithSummary("gets customer by id")
            .WithDescription("gets customer by id")
            .Produces<CustomerResponse>()
            .RequirePermission("Permissions.Customers.View")
            .MapToApiVersion(1);
    }
}
