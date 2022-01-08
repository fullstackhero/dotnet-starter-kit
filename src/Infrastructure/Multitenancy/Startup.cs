using DN.WebApi.Application.Multitenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Multitenancy;

internal static class Startup
{
    internal static IServiceCollection AddMultitenancy(this IServiceCollection services) =>
        services
            .AddScoped<CurrentTenantMiddleware>()
            .AddTransient<IMakeSecureConnectionString, MakeSecureConnectionString>();

    internal static IApplicationBuilder UseCurrentTenant(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentTenantMiddleware>();
}