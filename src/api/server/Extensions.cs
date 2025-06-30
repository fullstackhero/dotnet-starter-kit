using System.Reflection;
using Asp.Versioning;
using Carter;
using FluentValidation;
using FSH.Starter.WebApi.Host;

namespace FSH.Starter.WebApi.Host;

internal static class Extensions
{
    public static WebApplicationBuilder RegisterModules(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Define module assemblies for Clean Architecture layers
        var assemblies = new Assembly[]
        {
            typeof(Program).Assembly, // Host assembly
            typeof(AuthController).Assembly // Controllers assembly
        };

        // Register validators (Clean Architecture - Application Layer)
        builder.Services.AddValidatorsFromAssemblies(assemblies);

        // Register MediatR (Clean Architecture - Application Layer)
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });

        // Register controllers (Presentation Layer) - Explicit assembly scanning
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly)
            .AddApplicationPart(typeof(Program).Assembly);

        // Add carter endpoint modules (for additional API endpoints)
        builder.Services.AddCarter();

        return builder;
    }

    public static WebApplication UseModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Map controllers directly (Presentation Layer - Clean Architecture)
        app.MapControllers();

        // Use carter for additional endpoints
        app.MapCarter();

        // Add a health check endpoint at root for basic connectivity
        app.MapGet("/", () => new { 
            Status = "Healthy", 
            Service = "FSH Starter API",
            Timestamp = DateTime.UtcNow,
            Architecture = "Clean Architecture with DDD"
        }).AllowAnonymous();

        // Add a simple API info endpoint
        app.MapGet("/api", () => new {
            Message = "FSH Starter API - Clean Architecture with DDD",
            Version = "1.0",
            Endpoints = new[]
            {
                "/api/v1/auth/test",
                "/api/v1/auth/login",
                "/api/v1/auth/register"
            }
        }).AllowAnonymous();

        // Add debug endpoint to check controller registration
        app.MapGet("/debug/controllers", (IServiceProvider serviceProvider) =>
        {
            try
            {
                var controllers = serviceProvider.GetServices<Microsoft.AspNetCore.Mvc.ControllerBase>()
                    .Select(c => c.GetType().Name)
                    .ToList();
                
                return Results.Ok(new { 
                    message = "Controllers found",
                    controllers = controllers,
                    assemblies = new [] {
                        typeof(Program).Assembly.FullName,
                        typeof(AuthController).Assembly.FullName
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { error = ex.ToString() });
            }
        }).AllowAnonymous();

        return app;
    }
}
