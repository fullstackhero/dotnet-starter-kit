using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Products.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeleteProductEndpoint
{
    internal static RouteHandlerBuilder MapProductDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
             {
                 await mediator.Send(new DeleteProductCommand(id));
                 return Results.NoContent();
             })
            .WithName(nameof(DeleteProductEndpoint))
            .WithSummary("deletes product by id")
            .WithDescription("deletes product by id")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Products.Delete")
            .MapToApiVersion(1);
    }
}
