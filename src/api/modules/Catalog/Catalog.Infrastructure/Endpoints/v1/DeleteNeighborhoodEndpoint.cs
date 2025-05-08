using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeleteNeighborhoodEndpoint
{
    internal static RouteHandlerBuilder MapNeighborhoodDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/neighborhoods/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeleteNeighborhoodCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeleteNeighborhoodEndpoint))
            .WithSummary("Deletes a Neighborhood by ID")
            .WithDescription("Deletes a Neighborhood by ID")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Neighborhoods.Delete")
            .MapToApiVersion(1);
    }
}
