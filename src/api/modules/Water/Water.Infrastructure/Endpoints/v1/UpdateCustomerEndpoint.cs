using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Customers.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class UpdateCustomerEndpoint
{
    internal static RouteHandlerBuilder MapCustomerUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/{id:guid}", async (Guid id, UpdateCustomerCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateCustomerEndpoint))
            .WithSummary("update a customer")
            .WithDescription("update a customer")
            .Produces<UpdateCustomerResponse>()
            .RequirePermission("Permissions.Customers.Update")
            .MapToApiVersion(1);
    }
}
