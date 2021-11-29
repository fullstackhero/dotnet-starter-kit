using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Common.Extensions;

public static class CorsExtensions
{
    private static string[] GetWithOrigins(CorsSettings corsSettings){
        var result = new string[0]
        .Union(corsSettings.Angular.Split(';').Where(o1 => string.IsNullOrWhiteSpace(o1) == false))
        .Union(corsSettings.Blazor.Split(';').Where(o2 => string.IsNullOrWhiteSpace(o2) == false))
        .ToArray();
        return result;
    }
    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        var corsSettings = services.GetOptions<CorsSettings>(nameof(CorsSettings));

        var withOrigins = GetWithOrigins(corsSettings);

        return services.AddCors(opt =>
        {
            opt.AddPolicy("CorsPolicy", policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins(withOrigins);
            });
        });
    }
}