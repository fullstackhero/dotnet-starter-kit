using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Products.Get.v1;
using FSH.Starter.WebApi.Catalog.Application.Products.GetList.v1;
using FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;

public static class GetProductsEndpoint
{
    internal static RouteHandlerBuilder MapGetProductsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/getlist", async (ISender mediator, [FromBody] BaseFilter filter) =>
            {
                var response = await mediator.Send(new GetProductsRequest(filter));
                return Results.Ok(response);
            })
            .WithName(nameof(GetProductsEndpoint))
            .WithSummary("Gets a list of products")
            .WithDescription("Gets a list of products with filtering support")
            .Produces<List<ProductDto>>()
            .RequirePermission("Permissions.Products.Search")
            .MapToApiVersion(1);
    }
}

