using System.ComponentModel;
using FSH.Framework.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Integration.Tests.Tests.Caching;

/// <summary>
/// End-to-end tests against a real Valkey container (the kit's cache engine; Redis-protocol
/// compatible) — guards against the in-memory-vs-distributed serialization divergence tracked at
/// dotnet/extensions#6063, where caches behave differently once an actual
/// <see cref="IDistributedCache"/> is wired. The unit-level Caching.Tests use the in-memory
/// fallback and would not catch a distributed-cache-specific regression.
/// </summary>
public sealed class HybridCacheRedisTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("valkey/valkey:9.1.0-alpine")
        .Build();

    public Task InitializeAsync() => _redis.StartAsync();

    public Task DisposeAsync() => _redis.DisposeAsync().AsTask();

    private (HybridCache cache, IDistributedCache distributedCache, ServiceProvider provider) CreateCache()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CachingOptions:Redis"] = _redis.GetConnectionString(),
            })
            .Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddHeroCaching(config);
        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<HybridCache>(),
                provider.GetRequiredService<IDistributedCache>(),
                provider);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_RoundTrip_Through_Redis()
    {
        var (cache, _, provider) = CreateCache();
        await using (provider)
        {
            var payload = new TestPayload("hello", 42);

            // First call invokes factory, caches in L1 + L2 (Redis).
            var stored = await cache.GetOrCreateAsync(
                "rt:1",
                payload,
                static (p, ct) => ValueTask.FromResult(p));

            // Second call on the same instance — L1 hit, factory must NOT run.
            var factoryRuns = 0;
            var fetched = await cache.GetOrCreateAsync(
                "rt:1",
                payload,
                (p, ct) => { Interlocked.Increment(ref factoryRuns); return ValueTask.FromResult(p); });

            stored.ShouldBe(payload);
            fetched.ShouldBe(payload);
            factoryRuns.ShouldBe(0);
        }
    }

    [Fact]
    public async Task SetAsync_Should_Persist_Bytes_To_Redis()
    {
        // Asserts that an entry written via HybridCache.SetAsync actually lands in Redis
        // (not just in L1) — guards against silent dead-letter behavior.
        var (cache, distributedCache, provider) = CreateCache();
        await using (provider)
        {
            await cache.SetAsync("rt:set", "persisted-value");

            // Read the underlying L2 directly to confirm bytes are present.
            var raw = await distributedCache.GetAsync("rt:set");
            raw.ShouldNotBeNull();
            raw!.Length.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public async Task RemoveByTagAsync_Should_Invalidate_Within_Same_Instance()
    {
        // Note: HybridCache tag invalidation is per-instance (no L1 backplane). This test
        // verifies the in-instance behavior; cross-node invalidation requires a custom
        // pub/sub backplane and is documented as a known limitation in docs/caching.mdx.
        var (cache, _, provider) = CreateCache();
        await using (provider)
        {
            await cache.SetAsync("rt:tag:a", "v1", tags: ["group-x"]);
            await cache.SetAsync("rt:tag:b", "v2", tags: ["group-x"]);

            await cache.RemoveByTagAsync("group-x");

            var aRuns = 0;
            var bRuns = 0;
            await cache.GetOrCreateAsync(
                "rt:tag:a",
                0,
                (s, ct) => { Interlocked.Increment(ref aRuns); return ValueTask.FromResult("reload-a"); });
            await cache.GetOrCreateAsync(
                "rt:tag:b",
                0,
                (s, ct) => { Interlocked.Increment(ref bRuns); return ValueTask.FromResult("reload-b"); });

            aRuns.ShouldBe(1);
            bRuns.ShouldBe(1);
        }
    }

    [Fact]
    public void Redis_Backend_Should_Implement_IBufferDistributedCache()
    {
        // Microsoft.Extensions.Caching.StackExchangeRedis 9.0+ implements IBufferDistributedCache
        // for zero-copy reads via HybridCache. If this regresses, every L2 read pays an extra
        // byte[] allocation. Lock it in.
        var (_, distributedCache, provider) = CreateCache();
        using (provider)
        {
            distributedCache.ShouldBeAssignableTo<IBufferDistributedCache>(
                "StackExchange.Redis cache must implement IBufferDistributedCache for HybridCache zero-copy reads.");
        }
    }

    [ImmutableObject(true)]
    private sealed record TestPayload(string Name, int Number);
}
