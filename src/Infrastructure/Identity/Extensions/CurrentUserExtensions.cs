using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Identity.Extensions;

public static class CurrentUserExtensions
{
    internal static IApplicationBuilder UseMiddlewareCurrentUser(this IApplicationBuilder app)
    {
        app.UseMiddleware<CurrentUserMiddleware>();
        return app;
    }

    internal static IServiceCollection AddMiddlewareCurrentUser(this IServiceCollection services)
    {
        services.AddScoped<CurrentUserMiddleware>();
        return services;
    }
}