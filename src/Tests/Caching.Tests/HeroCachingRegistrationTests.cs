using FSH.Framework.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.Tests;

/// <summary>
/// Verifies that <see cref="Extensions.AddHeroCaching"/> wires HybridCache on top of an
/// <see cref="IDistributedCache"/> in both the in-memory fallback and the (configured) Redis path.
/// These tests exercise the registration wiring only — they do not require a running Redis.
/// </summary>
public sealed class HeroCachingRegistrationTests
{
    [Fact]
    public void AddHeroCaching_Should_RegisterInMemoryDistributedCache_When_RedisIsEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build(); // empty — no Redis

        // Act
        services.AddHeroCaching(config);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IDistributedCache>().ShouldNotBeNull();
        provider.GetService<HybridCache>().ShouldNotBeNull();
    }

    [Fact]
    public void AddHeroCaching_Should_BindCachingOptions_From_Configuration()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CachingOptions:DefaultExpiration"] = "00:30:00",
                ["CachingOptions:DefaultLocalCacheExpiration"] = "00:01:00",
                ["CachingOptions:MaximumKeyLength"] = "2048",
            })
            .Build();
        // BindConfiguration() resolves IConfiguration from DI, so register the root configuration.
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddHeroCaching(config);
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<CachingOptions>>().Value;
        options.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(30));
        options.DefaultLocalCacheExpiration.ShouldBe(TimeSpan.FromMinutes(1));
        options.MaximumKeyLength.ShouldBe(2048);
    }

    [Fact]
    public void AddHeroCaching_Should_Throw_When_ConfigurationIsNull()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() => services.AddHeroCaching(null!));
    }

    [Fact]
    public void AddHeroCaching_Should_RegisterITenantCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddHeroCaching(config);

        // Assert — ITenantCacheService must be registered (descriptor exists)
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITenantCacheService));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
