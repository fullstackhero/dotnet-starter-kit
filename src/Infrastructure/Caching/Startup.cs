using DN.WebApi.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Caching;

internal static class Startup
{
    internal static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration config)
    {
        var settings = config.GetSection(nameof(CacheSettings)).Get<CacheSettings>();

        if (settings.PreferRedis)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = settings.RedisURL;
                options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
                {
                    AbortOnConnectFail = true
                };
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services.AddSingleton<ICacheService, CacheService>();
    }
}