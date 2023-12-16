using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.WebApi.Modules.Catalog;

public static class CatalogModule
{
    public static WebApplication UseCatalogModule(this WebApplication app)
    {
        app.MapCatalogEndpoints();
        return app;
    }
    private static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        //var catalog = endpoints.NewVersionedApi();
        //var catalogV1 = catalog.MapGroup("catalog").HasApiVersion(1.0);
        var catalogEndpoints = endpoints.MapGroup("catalog");
        catalogEndpoints.MapProductEndpoints();
        return catalogEndpoints;
    }
}
