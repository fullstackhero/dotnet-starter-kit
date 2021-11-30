using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Common.Extensions;

public static class CorsExtensions
{
    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        var corsSettings = services.GetOptions<CorsSettings>(nameof(CorsSettings));
        var origins = new List<string>();
        if (corsSettings.Angular is not null) origins.Add(corsSettings.Angular);
        if (corsSettings.Blazor is not null) origins.Add(corsSettings.Blazor);

        return services.AddCors(opt => opt
            .AddPolicy("CorsPolicy", policy => policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithOrigins(origins.ToArray())));
    }
}