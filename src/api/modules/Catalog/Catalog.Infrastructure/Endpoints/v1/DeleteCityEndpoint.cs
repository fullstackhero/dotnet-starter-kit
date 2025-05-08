using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Cities.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeleteCityEndpoint
{
    internal static RouteHandlerBuilder MapCityDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/cities/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeleteCityCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeleteCityEndpoint))
            .WithSummary("Deletes a City by ID")
            .WithDescription("Deletes a City by ID")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Cities.Delete")
            .MapToApiVersion(1);
    }
}
