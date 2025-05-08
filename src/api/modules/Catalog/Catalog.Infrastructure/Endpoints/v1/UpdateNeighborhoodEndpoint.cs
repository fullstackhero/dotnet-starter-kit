using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class UpdateNeighborhoodEndpoint
{
    internal static RouteHandlerBuilder MapNeighborhoodUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/neighborhoods/{id:guid}", async (Guid id, UpdateNeighborhoodCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateNeighborhoodEndpoint))
            .WithSummary("Updates a Neighborhood")
            .WithDescription("Updates a Neighborhood")
            .Produces<UpdateNeighborhoodResponse>()
            .RequirePermission("Permissions.Neighborhoods.Update")
            .MapToApiVersion(1);
    }
}