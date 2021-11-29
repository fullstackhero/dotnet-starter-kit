using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Common.Extensions;

public static class CorsExtensions
{
    private static string[] GetWithOrigins(CorsSettings corsSettings)
    {
        string[] result = new string[0] { };
        if (corsSettings.Angular != null)
        {
            result.Union(corsSettings.Angular.Split(';').Where(o1 => string.IsNullOrWhiteSpace(o1) == false));
        }

        if (corsSettings.Blazor != null)
        {
            result.Union(corsSettings.Blazor.Split(';').Where(o2 => string.IsNullOrWhiteSpace(o2) == false));
        }

        return result.ToArray();
    }

    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        var corsSettings = services.GetOptions<CorsSettings>(nameof(CorsSettings));

        string[] withOrigins = GetWithOrigins(corsSettings);

        return services.AddCors(opt =>
        {
            opt.AddPolicy("CorsPolicy", policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins(withOrigins);
            });
        });
    }
}