using FSH.Framework.Core.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FSH.Framework.Infrastructure.Caching;
internal static class Extensions
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Extensions));
    internal static IServiceCollection ConfigureCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(nameof(CacheOptions)).Get<CacheOptions>();
        if (cacheOptions == null) return services;

        if (!string.IsNullOrEmpty(cacheOptions.Redis))
        {
            _logger.Information("configuring redis cache.");
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.Redis;
                options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
                {
                    AbortOnConnectFail = true,
                    EndPoints = { cacheOptions.Redis! }
                };
            });
        }
        else
        {
            _logger.Information("configuring memory cache.");
            services.AddDistributedMemoryCache();
        }

        services.AddTransient<ICacheService, DistributedCacheService>();

        return services;
    }
}
