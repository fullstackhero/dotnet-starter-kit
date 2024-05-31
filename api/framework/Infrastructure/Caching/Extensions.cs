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
        services.AddTransient<ICacheService, DistributedCacheService>();
        var cacheOptions = configuration.GetSection(nameof(CacheOptions)).Get<CacheOptions>();
        if (cacheOptions == null || string.IsNullOrEmpty(cacheOptions.Redis))
        {
            _logger.Information("configuring memory cache.");
            services.AddDistributedMemoryCache();
            return services;
        }

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

        return services;
    }
}
