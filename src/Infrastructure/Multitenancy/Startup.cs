using FSH.WebApi.Application.Multitenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Infrastructure.Multitenancy;

internal static class Startup
{
    internal static IServiceCollection AddMultitenancy(this IServiceCollection services) =>
        services
            .AddScoped<CurrentTenantMiddleware>()
            .AddScoped<ICurrentTenant, CurrentTenant>()
            .AddScoped(sp => (ICurrentTenantInitializer)sp.GetRequiredService<ICurrentTenant>())
            .AddTransient<IMakeSecureConnectionString, MakeSecureConnectionString>();

    internal static IApplicationBuilder UseCurrentTenant(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentTenantMiddleware>();
}