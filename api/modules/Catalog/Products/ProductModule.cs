using FSH.WebApi.Framework;
using FSH.WebApi.Modules.Catalog.Products.Features.v1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Modules.Catalog;

public static class ProductModule
{
    public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        return builder;
    }

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var products = endpoints.MapGroup("products");
        products.MapCreateProductEndpoint();
        return endpoints;
    }
}
