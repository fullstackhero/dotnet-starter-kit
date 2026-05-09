using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Customers.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class DeleteCustomerEndpoint
{
    internal static RouteHandlerBuilder MapCustomerDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
             {
                 await mediator.Send(new DeleteCustomerCommand(id));
                 return Results.NoContent();
             })
            .WithName(nameof(DeleteCustomerEndpoint))
            .WithSummary("deletes customer by id")
            .WithDescription("deletes customer by id")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Customers.Delete")
            .MapToApiVersion(1);
    }
}
