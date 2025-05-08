using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetNeighborhoodEndpoint
{
    internal static RouteHandlerBuilder MapGetNeighborhoodEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/neighborhoods/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetNeighborhoodRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetNeighborhoodEndpoint))
            .WithSummary("Gets a Neighborhood by ID")
            .WithDescription("Gets a Neighborhood by ID")
            .Produces<NeighborhoodResponse>()
            .RequirePermission("Permissions.Neighborhoods.View")
            .MapToApiVersion(1);
    }
}