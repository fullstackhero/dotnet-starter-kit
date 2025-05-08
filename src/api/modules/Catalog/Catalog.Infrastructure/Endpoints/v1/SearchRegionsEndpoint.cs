using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Regions.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.Regions.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class SearchRegionsEndpoint
{
    internal static RouteHandlerBuilder MapSearchRegionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/regions/search", async (ISender mediator, [FromBody] SearchRegionsCommand command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchRegionsEndpoint))
            .WithSummary("Searches Regions with pagination and filtering")
            .WithDescription("Searches Regions with pagination and filtering")
            .Produces<PagedList<RegionResponse>>()
            .RequirePermission("Permissions.Regions.View")
            .MapToApiVersion(1);
    }
}
