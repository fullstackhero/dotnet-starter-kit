using FSH.WebAPI.Infrastructure.Auth;
using FSH.WebAPI.Infrastructure.BackgroundJobs;
using FSH.WebAPI.Infrastructure.Caching;
using FSH.WebAPI.Infrastructure.Common;
using FSH.WebAPI.Infrastructure.Cors;
using FSH.WebAPI.Infrastructure.FileStorage;
using FSH.WebAPI.Infrastructure.Localization;
using FSH.WebAPI.Infrastructure.Mailing;
using FSH.WebAPI.Infrastructure.Mapping;
using FSH.WebAPI.Infrastructure.Middleware;
using FSH.WebAPI.Infrastructure.Multitenancy;
using FSH.WebAPI.Infrastructure.Notifications;
using FSH.WebAPI.Infrastructure.OpenApi;
using FSH.WebAPI.Infrastructure.Persistence;
using FSH.WebAPI.Infrastructure.SecurityHeaders;
using FSH.WebAPI.Infrastructure.Seeding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebAPI.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MapsterSettings.Configure();
        return services
            .AddApiVersioning()
            .AddAuth(config)
            .AddBackgroundJobs(config)
            .AddCaching(config)
            .AddCorsPolicy(config)
            .AddExceptionMiddleware()
            .AddHealthCheck()
            .AddLocalization(config)
            .AddMailing(config)
            .AddMultitenancy()
            .AddNotifications(config)
            .AddOpenApiDocumentation(config)
            .AddPersistence(config)
            .AddRequestLogging(config)
            .AddRouting(options => options.LowercaseUrls = true)
            .AddSeeders()
            .AddServices();
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder appBuilder, IConfiguration config) =>
        appBuilder
            .UseLocalization(config)
            .UseStaticFiles()
            .UseSecurityHeaders(config)
            .UseFileStorage()
            .UseExceptionMiddleware()
            .UseLocalization(config)
            .UseRouting()
            .UseCorsPolicy()
            .UseAuthentication()
            .UseCurrentUser()
            .UseCurrentTenant()
            .UseAuthorization()
            .UseRequestLogging(config)
            .UseHangfireDashboard(config)
            .UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireAuthorization();
                endpoints.MapHealthCheck();
                endpoints.MapNotifications();
            })
            .UseOpenApiDocumentation(config);

    private static IServiceCollection AddApiVersioning(this IServiceCollection services) =>
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        });

    private static IServiceCollection AddHealthCheck(this IServiceCollection services) =>
        services.AddHealthChecks().AddCheck<TenantHealthCheck>("Tenant").Services;

    private static IEndpointConventionBuilder MapHealthCheck(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHealthChecks("/api/health").RequireAuthorization();
}