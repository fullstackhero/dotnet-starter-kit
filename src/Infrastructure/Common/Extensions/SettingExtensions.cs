using DN.WebApi.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Common.Extensions;

public static class SettingExtensions
{
    internal static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration config)
    {
        services
            .Configure<MailSettings>(config.GetSection(nameof(MailSettings)))
            .Configure<MiddlewareSettings>(config.GetSection(nameof(MiddlewareSettings)))
            .Configure<CorsSettings>(config.GetSection(nameof(CorsSettings)))
            .Configure<CacheSettings>(config.GetSection(nameof(CacheSettings)))
            .Configure<SwaggerSettings>(config.GetSection(nameof(SwaggerSettings)));
        return services;
    }
}