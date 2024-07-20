using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Cors;
public static class Extensions
{
    private const string CorsPolicy = nameof(CorsPolicy);
    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var corsOptions = config.GetSection(nameof(CorsOptions)).Get<CorsOptions>();
        if (corsOptions == null) { return services; }
        return services.AddCors(opt =>
        opt.AddPolicy(CorsPolicy, policy =>
            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithOrigins(corsOptions.AllowedOrigins.ToArray())));
    }

    internal static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        return app.UseCors(CorsPolicy);
    }
}
