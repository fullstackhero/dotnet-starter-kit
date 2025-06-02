using System.Reflection;
using Asp.Versioning.Conventions;
using Carter;
using FluentValidation;

namespace FSH.Starter.WebApi.Host;

internal static class Extensions
{
    public static WebApplicationBuilder RegisterModules(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Define module assemblies
        var assemblies = new Assembly[]
        {
            typeof(Program).Assembly
        };

        // Register validators
        builder.Services.AddValidatorsFromAssemblies(assemblies);

        // Register mediatr
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });

        // Register controllers
        builder.Services.AddControllers();

        // Add carter endpoint modules
        builder.Services.AddCarter();

        // Register HttpClient for MERNİS service
        builder.Services.AddHttpClient<IMernisService, MernisService>();

        // Register MERNİS service
        builder.Services.AddSingleton<IMernisService, MernisService>();

        // Register Password Reset service
        builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

        return builder;
    }

    public static WebApplication UseModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Register api versions
        var versions = app.NewApiVersionSet()
                    .HasApiVersion(1)
                    .HasApiVersion(2)
                    .ReportApiVersions()
                    .Build();

        // Map versioned endpoint
        var endpoints = app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(versions);

        // Use carter
        endpoints.MapCarter();

        // Map controllers
        app.MapControllers();

        return app;
    }
}
