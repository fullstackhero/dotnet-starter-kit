using System.Reflection;
using System.Runtime.CompilerServices;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Auth;
using FL_CRMS_ERP_WEBAPI.Infrastructure.BackgroundJobs;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Caching;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Common;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Cors;
using FL_CRMS_ERP_WEBAPI.Infrastructure.FileStorage;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Localization;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Mailing;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Mapping;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Middleware;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Multitenancy;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Notifications;
using FL_CRMS_ERP_WEBAPI.Infrastructure.OpenApi;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Initialization;
using FL_CRMS_ERP_WEBAPI.Infrastructure.SecurityHeaders;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Validations;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Infrastructure.Test")]

namespace FL_CRMS_ERP_WEBAPI.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var applicationAssembly = typeof(FL_CRMS_ERP_WEBAPI.Application.Startup).GetTypeInfo().Assembly;
        MapsterSettings.Configure();
        return services
            .AddApiVersioning()
            .AddAuth(config)
            .AddBackgroundJobs(config)
            .AddCaching(config)
            .AddCorsPolicy(config)
            .AddExceptionMiddleware()
            .AddBehaviours(applicationAssembly)
            .AddHealthCheck()
            .AddPOLocalization(config)
            .AddMailing(config)
            .AddMediatR(Assembly.GetExecutingAssembly())
            .AddMultitenancy()
            .AddNotifications(config)
            .AddOpenApiDocumentation(config)
            .AddPersistence()
            .AddRequestLogging(config)
            .AddRouting(options => options.LowercaseUrls = true)
            .AddServices();
    }

    private static IServiceCollection AddApiVersioning(this IServiceCollection services) =>
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        });

    private static IServiceCollection AddHealthCheck(this IServiceCollection services) =>
        services.AddHealthChecks().AddCheck<TenantHealthCheck>("Tenant").Services;

    public static async Task InitializeDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // Create a new scope to retrieve scoped services
        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
            .InitializeDatabasesAsync(cancellationToken);
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder builder, IConfiguration config) =>
        builder
            .UseRequestLocalization()
            .UseStaticFiles()
            .UseSecurityHeaders(config)
            .UseFileStorage()
            .UseExceptionMiddleware()
            .UseRouting()
            .UseCorsPolicy()
            .UseAuthentication()
            .UseCurrentUser()
            .UseMultiTenancy()
            .UseAuthorization()
            .UseRequestLogging(config)
            .UseHangfireDashboard(config)
            .UseOpenApiDocumentation(config);

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers().RequireAuthorization();
        builder.MapHealthCheck();
        builder.MapNotifications();
        return builder;
    }

    private static IEndpointConventionBuilder MapHealthCheck(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHealthChecks("/api/health");
}