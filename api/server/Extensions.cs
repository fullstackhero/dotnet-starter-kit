using System.Reflection;
using Asp.Versioning.Conventions;
using Carter;
using FluentValidation;
using FSH.Framework.Core;
using FSH.Framework.Infrastructure.Behaviours;
using FSH.Framework.Infrastructure.Multitenancy;
using FSH.Framework.Infrastructure.OpenApi;
using FSH.WebApi.Catalog.Application;
using FSH.WebApi.Catalog.Infrastructure;
using FSH.WebApi.Todo;
using MediatR;

namespace FSH.WebApi.Server;

public static class Extensions
{
    public static WebApplicationBuilder AddModules(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        //define module assemblies
        var assemblies = new Assembly[]
        {
            typeof(FshCore).Assembly,
            typeof(CatalogApplication).Assembly,
            typeof(TodoApplication).Assembly
        };

        //register validators
        builder.Services.AddValidatorsFromAssemblies(assemblies);

        //register mediatr
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        //register module services
        builder.RegisterCatalogServices();
        builder.RegisterTodoServices();

        //add carter endpoint modules
        builder.Services.AddCarter(configurator: config =>
        {
            config.WithModule<CatalogModule.Endpoints>();
            config.WithModule<MutitenancyModule.Endpoints>();
            config.WithModule<TodoModule.Endpoints>();
        });

        return builder;
    }

    public static WebApplication UseModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        //register modules
        app.UseCatalogModule();
        app.UseTodoModule();

        //register api versions
        var versions = app.NewApiVersionSet()
                    .HasApiVersion(1)
                    .HasApiVersion(2)
                    .ReportApiVersions()
                    .Build();

        //map versioned endpoint
        var endpoints = app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(versions);

        //register dummy endpoints
        endpoints.MapGet("/", () => "hello earth!").WithTags("hello").HasApiVersion(1);
        endpoints.MapGet("/", () => "hello world!").WithTags("hello").HasApiVersion(2);

        //use carter
        endpoints.MapCarter();

        //use open api
        app.UseOpenApi();

        return app;
    }
}
