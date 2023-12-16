using FSH.WebApi.Modules.Catalog;
using Wolverine;

namespace FSH.WebApi.Server;

public static class Extensions
{
    public static WebApplicationBuilder AddModules(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        //register wolverine with module assemblies
        builder.Host.UseWolverine(options =>
        {
            options.Discovery.IncludeAssembly(typeof(CatalogModule).Assembly);
        });
        return builder;
    }

    public static WebApplication UseModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        //register modules
        app.UseCatalogModule();
        return app;
    }
}
