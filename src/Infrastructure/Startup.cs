using DN.WebApi.Infrastructure.Caching;
using DN.WebApi.Infrastructure.Common;
using DN.WebApi.Infrastructure.Cors;
using DN.WebApi.Infrastructure.FileStorage;
using DN.WebApi.Infrastructure.Hangfire;
using DN.WebApi.Infrastructure.Identity;
using DN.WebApi.Infrastructure.Localization;
using DN.WebApi.Infrastructure.Mailing;
using DN.WebApi.Infrastructure.Mapping;
using DN.WebApi.Infrastructure.Middleware;
using DN.WebApi.Infrastructure.Multitenancy;
using DN.WebApi.Infrastructure.Notifications;
using DN.WebApi.Infrastructure.Seeding;
using DN.WebApi.Infrastructure.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MapsterSettings.Configure();
        return services
            .AddApiVersioning()
            .AddCaching(config)
            .AddCorsPolicy(config)
            .AddCurrentUser()
            .AddCurrentTenant()
            .AddExceptionMiddleware()
            .AddHangfire(config)
            .AddHealthCheck()
            .AddIdentity(config)
            .AddLocalization(config)
            .AddMailing(config)
            .AddNotifications(config)
            .AddPermissions()
            .AddRequestLogging(config)
            .AddRouting(options => options.LowercaseUrls = true)
            .AddSeeders()
            .AddServices()
            .AddSwaggerDocumentation(config)
            .AddMultitenancy(config); // Multitency needs to be last as this one also creates and/or migrates the database(s).
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder appBuilder, IConfiguration config) =>
        appBuilder
            .UseLocalization(config)
            .UseStaticFiles()
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
            .UseSwaggerDocumentation(config);

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