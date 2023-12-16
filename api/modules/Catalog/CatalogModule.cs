using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Modules.Catalog;

public static class CatalogModule
{
    public static WebApplicationBuilder AddCatalogServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }
    public static WebApplication UseCatalogModule(this WebApplication app)
    {
        return app;
    }
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var catalogEndpoints = endpoints
            .MapGroup("catalog")
            .MapProductEndpoints();
        return catalogEndpoints;
    }
}
