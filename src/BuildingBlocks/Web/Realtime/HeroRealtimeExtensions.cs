using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Web.Realtime;

public static class HeroRealtimeExtensions
{
    /// <summary>
    /// Registers SignalR with a Redis backplane when <c>CachingOptions:Redis</c> is configured.
    /// Without Redis the hub still works in single-host mode (useful for tests / dev).
    /// Also registers the in-memory presence tracker shared by the hub and the presence endpoint.
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

        services.AddSingleton<IPresenceTracker, PresenceTracker>();

        return services;
    }

    public static IEndpointRouteBuilder MapHeroRealtime(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapHub<AppHub>("/api/v1/realtime/hub");

        // Snapshot endpoint — clients poll this for initial state when their
        // session hadn't observed the PresenceChanged broadcasts yet.
        endpoints.MapGet("/api/v1/realtime/presence",
                ([FromQuery] string userIds, IPresenceTracker presence) =>
                {
                    var ids = (userIds ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var map = presence.GetStatus(ids);
                    return Results.Ok(map.Select(kv => new { userId = kv.Key, online = kv.Value }));
                })
            .RequireAuthorization()
            .WithName("GetPresence")
            .WithSummary("Snapshot online status for a comma-separated list of user ids.");

        return endpoints;
    }
}
