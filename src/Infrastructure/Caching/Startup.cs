using FSH.WebApi.Application.Common.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FSH.WebApi.Infrastructure.Caching;

internal static class Startup
{
    private static readonly ILogger Logger = Log.ForContext(typeof(Startup));
    internal static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration config)
    {
        var settings = config.GetSection(nameof(CacheSettings)).Get<CacheSettings>();

        if (settings.UseDistributedCache)
        {
            if (settings.PreferRedis)
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = settings.RedisURL;
                    options.ConfigurationOptions = new ConfigurationOptions()
                    {
                        AbortOnConnectFail = true,
                        EndPoints = { settings.RedisURL }
                    };
                    Logger.Information($"Using Redis distributed cache : {settings.RedisURL} , if using redis stack, default management UI is http://localhost:8001");
                });
            }
            else
            {
                Logger.Information("Using distributed memory cache");
                services.AddDistributedMemoryCache();
            }

            services.AddTransient<ICacheService, DistributedCacheService>();
        }
        else
        {
            Logger.Information("Using local memory cache");
            services.AddMemoryCache();
            services.AddTransient<ICacheService, LocalCacheService>();
        }

        return services;
    }
}
