using DN.WebApi.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Extensions;

public static class TenantExtensions
{
    internal static IApplicationBuilder UseMiddlewareTenant(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantMiddleware>();
        return app;
    }

    internal static IServiceCollection AddMiddlewareTenant(this IServiceCollection services)
    {
        services.AddScoped<TenantMiddleware>();
        return services;
    }
}