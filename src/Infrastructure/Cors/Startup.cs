using FSH.WebApi.Infrastructure.Common.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Infrastructure.Cors;

internal static class Startup
{
    private const string CorsPolicy = nameof(CorsPolicy);

    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        // var corsSettings = config.GetSection(nameof(CorsSettings)).Get<CorsSettings>();
        // if (corsSettings == null) return services;
        // var origins = new List<string>();
        // if (corsSettings.Angular is not null)
        //     origins.AddRange(corsSettings.Angular.Split(';', StringSplitOptions.RemoveEmptyEntries));
        // if (corsSettings.Blazor is not null)
        //     origins.AddRange(corsSettings.Blazor.Split(';', StringSplitOptions.RemoveEmptyEntries));
        // if (corsSettings.React is not null)
        //     origins.AddRange(corsSettings.React.Split(';', StringSplitOptions.RemoveEmptyEntries));
        // if (corsSettings.Flutter is not null)
        //     origins.AddRange(corsSettings.Flutter.Split(';', StringSplitOptions.RemoveEmptyEntries));

        return services.AddCors(opt =>
            opt.AddDefaultPolicy(
                 builder =>
                 {
                     builder
                         .AllowAnyHeader()
                         .AllowAnyMethod()
                         .AllowAnyOrigin();
                     //.AllowCredentials();
                 }));
    }

    // internal static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) =>
    //     app.UseCors(CorsPolicy);
}