using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Modules.Catalog;

public static class CatalogModule
{
    public static WebApplication UseCatalogModule(this WebApplication app)
    {
        return app;
    }
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var catalogEndpoints = endpoints.MapGroup("v{version:apiVersion}/catalog")
            .MapProductEndpoints();
        return catalogEndpoints;
    }
}
