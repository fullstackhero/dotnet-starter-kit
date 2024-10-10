using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public static class SearchDimensionsEndpoint
{
    internal static RouteHandlerBuilder MapSearchDimensionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/search", async (ISender mediator, [FromBody] SearchDimensionsRequest command) =>
        {
            var response = await mediator.Send(command);
            return Results.Ok(response);
        })
        .WithName(nameof(SearchDimensionsEndpoint))
        .WithSummary("Gets a list of dimension items with paging support")
        .WithDescription("Gets a list of dimension items with paging support")
        .Produces<PagedList<DimensionDto>>()
        .RequirePermission("Permissions.Dimensions.View")
        .MapToApiVersion(1);
    }
}
