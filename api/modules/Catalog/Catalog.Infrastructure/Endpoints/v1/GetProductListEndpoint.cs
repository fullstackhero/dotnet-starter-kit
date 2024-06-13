using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.WebApi.Catalog.Application.Products.Get.v1;
using FSH.WebApi.Catalog.Application.Products.GetList.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetProductListEndpoint
{
    internal static RouteHandlerBuilder MapGetProductListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/", async (ISender mediator, int pageNumber = 1, int pageSize = 10) =>
            {
                var response = await mediator.Send(new GetProductListRequest(pageNumber, pageSize));
                return Results.Ok(response);
            })
            .WithName(nameof(GetProductListEndpoint))
            .WithSummary("gets a list of products")
            .WithDescription("gets a list of products")
            .Produces<PagedList<GetProductResponse>>()
            .RequirePermission("Permissions.Products.View")
            .MapToApiVersion(1);
    }
}
