using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;

public static class SearchProductsEndpoint
{
    internal static RouteHandlerBuilder MapSearchProductsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/search", async (ISender mediator, [FromBody] SearchProductsRequest command) =>
            {
                var response = await mediator.Send(command);
                return Results.Ok(response);
            })
            .WithName(nameof(SearchProductsEndpoint))
            .WithSummary("Gets a pagination of products")
            .WithDescription("Gets a list of products with pagination and filtering support")
            .Produces<PagedList<ProductDto>>()
            .RequirePermission("Permissions.Products.Search")
            .MapToApiVersion(1);
    }
}

