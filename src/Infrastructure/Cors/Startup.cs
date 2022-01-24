using FSH.WebApi.Infrastructure.Common.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Diagnostics;

namespace FSH.WebApi.Infrastructure.Cors;

internal static class Startup
{
    private const string CorsPolicy = nameof(CorsPolicy);

    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var corsSettings = config.GetSection(nameof(CorsSettings)).Get<CorsSettings>();
        var origins = new List<string>();
        if (corsSettings.Angular is not null)
            origins.AddRange(corsSettings.Angular.Split(';', StringSplitOptions.RemoveEmptyEntries));
        if (corsSettings.Blazor is not null)
            origins.AddRange(corsSettings.Blazor.Split(';', StringSplitOptions.RemoveEmptyEntries));
        if (corsSettings.React is not null)
            origins.AddRange(corsSettings.React.Split(';', StringSplitOptions.RemoveEmptyEntries));

        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        origins.ForEach(o => Log.Information("[CORS] added for {o}", o));
        return services.AddCors(opt =>
            opt.AddPolicy(CorsPolicy, policy =>
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithOrigins(origins.ToArray())));
    }

    internal static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) =>
        app.UseCors(CorsPolicy);
}