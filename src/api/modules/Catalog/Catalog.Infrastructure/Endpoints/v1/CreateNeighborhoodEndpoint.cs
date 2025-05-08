using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class CreateNeighborhoodEndpoint
{
    internal static RouteHandlerBuilder MapNeighborhoodCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/neighborhoods", async (CreateNeighborhoodCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateNeighborhoodEndpoint))
            .WithSummary("Creates a Neighborhood")
            .WithDescription("Creates a Neighborhood")
            .Produces<CreateNeighborhoodResponse>()
            .RequirePermission("Permissions.Neighborhoods.Create")
            .MapToApiVersion(1);
    }
}