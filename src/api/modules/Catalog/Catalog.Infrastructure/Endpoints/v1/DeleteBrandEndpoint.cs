using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Brands.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeleteBrandEndpoint
{
    internal static RouteHandlerBuilder MapBrandDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
             {
                 await mediator.Send(new DeleteBrandCommand(id));
                 return Results.NoContent();
             })
            .WithName(nameof(DeleteBrandEndpoint))
            .WithSummary("deletes brand by id")
            .WithDescription("deletes brand by id")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Brands.Delete")
            .MapToApiVersion(1);
    }
}
