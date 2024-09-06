using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public static class GetDimensionListEndpoint
{
    internal static RouteHandlerBuilder MapGetDimensionListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/search", async (ISender mediator, [FromBody] PaginationFilter filter) =>
        {
            var response = await mediator.Send(new GetDimensionListRequest(filter));
            return Results.Ok(response);
        })
        .WithName(nameof(GetDimensionListEndpoint))
        .WithSummary("Gets a list of dimension items with paging support")
        .WithDescription("Gets a list of dimension items with paging support")
        .Produces<PagedList<DimensionDto>>()
        .RequirePermission("Permissions.Dimensions.View")
        .MapToApiVersion(1);
    }
}
