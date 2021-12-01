using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Caching;
using DN.WebApi.Infrastructure.Hangfire;
using DN.WebApi.Infrastructure.Identity;
using DN.WebApi.Infrastructure.Localization;
using DN.WebApi.Infrastructure.Mappings;
using DN.WebApi.Infrastructure.Middlewares;
using DN.WebApi.Infrastructure.Multitenancy;
using DN.WebApi.Infrastructure.Notifications;
using DN.WebApi.Infrastructure.Seeders;
using DN.WebApi.Infrastructure.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace DN.WebApi.Infrastructure;

public static class Startup
{
    private const string CorsPolicy = nameof(CorsPolicy);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MapsterSettings.Configure();
        services.AddCaching(config);
        services.AddSeeders();
        services.AddCurrentUser();
        services.AddCurrentTenant();
        services.AddHealthCheck();
        services.AddLocalization(config);
        services.AddServices();
        services.AddSettings(config);
        services.AddPermissions();
        services.AddIdentity(config);
        services.AddHangfire(config);
        services.AddMultitenancy(config);
        services.AddRouting(options => options.LowercaseUrls = true);
        services.AddScoped<ExceptionMiddleware>();
        services.AddRequestLogging(config);
        services.AddSwaggerDocumentation(config);
        services.AddCorsPolicy(config);
        services.AddApiVersioning();
        services.AddNotifications(config);
        return services;
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app, IConfiguration config)
    {
        app.UseLocalization(config);
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Files")),
            RequestPath = new PathString("/Files")
        });
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseRequestLogging(config);
        app.UseLocalization(config);
        app.UseRouting();
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.UseCurrentUser();
        app.UseCurrentTenant();
        app.UseAuthorization();
        app.UseHangfireDashboard(config);
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();
            endpoints.MapHealthChecks("/api/health").RequireAuthorization();
            endpoints.MapNotifications();
        });
        app.UseSwaggerDocumentation(config);
        return app;
    }

    private static IServiceCollection AddHealthCheck(this IServiceCollection services) =>
        services.AddHealthChecks().AddCheck<TenantHealthCheck>("Tenant").Services;

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        var transientServiceType = typeof(ITransientService);
        var transientServices = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => transientServiceType.IsAssignableFrom(p))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Service = t.GetInterfaces().FirstOrDefault(),
                Implementation = t
            })
            .Where(t => t.Service != null);

        foreach (var transientService in transientServices)
        {
            if (transientServiceType.IsAssignableFrom(transientService.Service))
            {
                services.AddTransient(transientService.Service, transientService.Implementation);
            }
        }

        var scopedServiceType = typeof(IScopedService);
        var scopedServices = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => scopedServiceType.IsAssignableFrom(p))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Service = t.GetInterfaces().FirstOrDefault(),
                Implementation = t
            })
            .Where(t => t.Service != null);

        foreach (var scopedService in scopedServices)
        {
            if (scopedServiceType.IsAssignableFrom(scopedService.Service))
            {
                services.AddScoped(scopedService.Service, scopedService.Implementation);
            }
        }

        return services;
    }

    internal static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration config) =>
        services.Configure<MailSettings>(config.GetSection(nameof(MailSettings)));

    private static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var corsSettings = config.GetSection(nameof(CorsSettings)).Get<CorsSettings>();
        var origins = new List<string>();
        if (corsSettings.Angular is not null)
            origins.AddRange(corsSettings.Angular.Split(';', StringSplitOptions.RemoveEmptyEntries));
        if (corsSettings.Blazor is not null)
            origins.AddRange(corsSettings.Blazor.Split(';', StringSplitOptions.RemoveEmptyEntries));

        return services.AddCors(opt =>
            opt.AddPolicy(CorsPolicy, policy =>
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithOrigins(origins.ToArray())));
    }

    internal static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app, IConfiguration config)
    {
        var middlewareSettings = config.GetSection(nameof(MiddlewareSettings)).Get<MiddlewareSettings>();
        if (middlewareSettings.EnableHttpsLogging)
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<ResponseLoggingMiddleware>();
        }

        return app;
    }

    internal static IServiceCollection AddRequestLogging(this IServiceCollection services, IConfiguration config)
    {
        var middlewareSettings = config.GetSection(nameof(MiddlewareSettings)).Get<MiddlewareSettings>();
        if (middlewareSettings.EnableHttpsLogging)
        {
            services.AddSingleton<RequestLoggingMiddleware>();
            services.AddSingleton<ResponseLoggingMiddleware>();
        }

        return services;
    }

    private static void AddApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        });
    }
}