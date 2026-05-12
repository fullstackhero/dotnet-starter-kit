using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Web.Realtime;

public static class HeroRealtimeExtensions
{
    /// <summary>
    /// Registers SignalR with a Redis backplane when <c>CachingOptions:Redis</c> is configured.
    /// Without Redis the hub still works in single-host mode (useful for tests / dev).
    /// </summary>
    public static IServiceCollection AddHeroRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var redis = configuration["CachingOptions:Redis"];
        var signalr = services.AddSignalR();
        if (!string.IsNullOrWhiteSpace(redis))
        {
            signalr.AddStackExchangeRedis(redis, options => options.Configuration.ChannelPrefix =
                StackExchange.Redis.RedisChannel.Literal("fsh-signalr"));
        }

        return services;
    }

    public static IEndpointRouteBuilder MapHeroRealtime(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapHub<AppHub>("/api/v1/realtime/hub");
        return endpoints;
    }
}
