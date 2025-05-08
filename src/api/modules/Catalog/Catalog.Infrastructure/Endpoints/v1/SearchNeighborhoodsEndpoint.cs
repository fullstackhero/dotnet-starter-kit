using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class SearchNeighborhoodsEndpoint
{
    internal static RouteHandlerBuilder MapSearchNeighborhoodsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/neighborhoods/search", async (ISender mediator, [FromBody] SearchNeighborhoodsCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchNeighborhoodsEndpoint))
            .WithSummary("Searches Neighborhoods with pagination and filtering")
            .WithDescription("Searches Neighborhoods with pagination and filtering")
            .Produces<PagedList<NeighborhoodResponse>>()
            .RequirePermission("Permissions.Neighborhoods.View")
            .MapToApiVersion(1);
    }
}
