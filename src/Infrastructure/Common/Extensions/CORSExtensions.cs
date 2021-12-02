using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Common.Extensions;

public static class CorsExtensions
{
    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        var corsSettings = services.GetOptions<CorsSettings>(nameof(CorsSettings));

        return services.AddCors(opt =>
            opt.AddPolicy("CorsPolicy", policy =>
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithOrigins(GetWithOrigins(corsSettings))));
    }

    private static string[] GetWithOrigins(CorsSettings corsSettings)
    {
        var result = new List<string>();
        if (corsSettings.Angular is not null)
        {
            result.AddRange(corsSettings.Angular.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        if (corsSettings.Blazor is not null)
        {
            result.AddRange(corsSettings.Blazor.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        return result.ToArray();
    }
}