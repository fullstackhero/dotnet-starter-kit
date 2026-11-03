using FSH.Framework.Quota;
using FSH.Framework.Web.Realtime;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework.Tests.Web;

/// <summary>
/// Guards the consumers of <c>CachingOptions:KeyPrefix</c> that do not go through
/// <c>IDistributedCache</c> (whose InstanceName the integration tests cover): the SignalR backplane
/// channel and the quota counters. Registration only — nothing here connects to Redis.
/// </summary>
public sealed class RedisKeyPrefixWiringTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static RedisOptions SignalRRedisOptions(Dictionary<string, string?> values)
    {
        var configuration = Config(values);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHeroRealtime(configuration);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<RedisOptions>>().Value;
    }

    [Fact]
    public void AddHeroRealtime_Should_NamespaceTheBackplaneChannel_When_KeyPrefixIsSet()
    {
        var options = SignalRRedisOptions(new()
        {
            ["CachingOptions:Redis"] = "localhost:6379",
            ["CachingOptions:KeyPrefix"] = "MyApp:prod",
        });

        // Pub/sub ignores the database index, so the channel prefix is the only thing keeping two
        // applications on one Redis server from receiving each other's hub broadcasts.
        options.Configuration.ChannelPrefix.ToString().ShouldBe("MyApp:prod:signalr");
    }

    [Fact]
    public void AddHeroRealtime_Should_KeepTheHistoricalChannel_When_KeyPrefixIsEmpty()
    {
        var options = SignalRRedisOptions(new()
        {
            ["CachingOptions:Redis"] = "localhost:6379",
        });

        options.Configuration.ChannelPrefix.ToString().ShouldBe("fsh-signalr");
    }

    [Fact]
    public void AddHeroQuotas_Should_InheritTheCachingKeyPrefix_When_QuotaHasNone()
    {
        var configuration = Config(new()
        {
            ["QuotaOptions:Enabled"] = "false",
            ["CachingOptions:KeyPrefix"] = "MyApp:prod",
        });
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddHeroQuotas(configuration);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<QuotaOptions>().KeyPrefix.ShouldBe("MyApp:prod");
    }

    [Fact]
    public void AddHeroQuotas_Should_PreferItsOwnKeyPrefix_When_Configured()
    {
        var configuration = Config(new()
        {
            ["QuotaOptions:Enabled"] = "false",
            ["QuotaOptions:KeyPrefix"] = "quota-store",
            ["CachingOptions:KeyPrefix"] = "MyApp:prod",
        });
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddHeroQuotas(configuration);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<QuotaOptions>().KeyPrefix.ShouldBe("quota-store");
    }
}
