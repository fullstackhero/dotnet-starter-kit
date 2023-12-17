using Asp.Versioning.Conventions;
using FSH.Framework.OpenApi;
using FSH.WebApi.Modules.Catalog;
using Wolverine;
using Wolverine.FluentValidation;

namespace FSH.WebApi.Server;

public static class Extensions
{
    public static WebApplicationBuilder AddModules(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        //register wolverine with module assemblies
        builder.Host.UseWolverine(options =>
        {
            options.CodeGeneration.TypeLoadMode = JasperFx.CodeGeneration.TypeLoadMode.Auto;
            options.Discovery.IncludeAssembly(typeof(CatalogModule).Assembly);
            options.UseFluentValidation();
        });

        //register module services
        builder.AddCatalogServices();
        return builder;
    }

    public static WebApplication UseModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        //register modules
        app.UseCatalogModule();

        //register api versions
        var versions = app.NewApiVersionSet()
                    .HasApiVersion(1)
                    .HasApiVersion(2)
                    .ReportApiVersions()
                    .Build();
        var endpoints = app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(versions);

        //register dummy endpoints
        endpoints.MapGet("/", () => "hello earth!").WithTags("hello").HasApiVersion(1);
        endpoints.MapGet("/", () => "hello world!").WithTags("hello").HasApiVersion(2);

        //register module endpoints
        endpoints.MapCatalogEndpoints();

        //register open api
        app.UseOpenApi();
        return app;
    }
}
