using System.ComponentModel;
using FSH.Framework.Caching;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
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

    private (HybridCache cache, IDistributedCache distributedCache, ServiceProvider provider) CreateCache(string? keyPrefix = null)
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CachingOptions:Redis"] = _redis.GetConnectionString(),
                ["CachingOptions:KeyPrefix"] = keyPrefix,
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
            raw.Length.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public async Task RemoveByTagAsync_Should_Invalidate_Within_Same_Instance()
    {
        // HybridCache tag invalidation is per-instance (no L1 backplane); cross-node invalidation
        // needs a custom pub/sub backplane — a known limitation documented in docs/caching.mdx.
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
        // StackExchangeRedis 9.0+ implements IBufferDistributedCache for HybridCache zero-copy reads;
        // a regression would cost an extra byte[] allocation per L2 read, so lock it in.
        var (_, distributedCache, provider) = CreateCache();
        using (provider)
        {
            distributedCache.ShouldBeAssignableTo<IBufferDistributedCache>(
                "StackExchange.Redis cache must implement IBufferDistributedCache for HybridCache zero-copy reads.");
        }
    }

    // ─── CachingOptions.KeyPrefix: two applications sharing one Redis server ─────────────

    [Fact]
    public async Task KeyPrefix_Should_Isolate_Entries_Between_Applications()
    {
        var (appA, _, providerA) = CreateCache("app-a:prod");
        var (appB, _, providerB) = CreateCache("app-b:prod");
        await using (providerA)
        await using (providerB)
        {
            await appA.SetAsync("theme:t:root", "from-a");

            var bRuns = 0;
            var fromB = await appB.GetOrCreateAsync(
                "theme:t:root",
                0,
                (s, ct) => { Interlocked.Increment(ref bRuns); return ValueTask.FromResult("from-b"); });

            fromB.ShouldBe("from-b");
            bRuns.ShouldBe(1, "app B must not read app A's entry for the same logical key");

            using var redis = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
            var db = redis.GetDatabase();
            (await db.KeyExistsAsync("app-a:prod:theme:t:root")).ShouldBeTrue();
            (await db.KeyExistsAsync("theme:t:root")).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task KeyPrefix_Should_Isolate_Tag_Invalidation_Between_Applications()
    {
        // HybridCache records tag invalidations in L2. Unprefixed, app A evicting "permissions"
        // also invalidates app B's permission entries on the same Redis server.
        var (appB, _, providerB) = CreateCache("app-b:prod");
        await using (providerB)
        {
            await appB.SetAsync("perm:u:1", "b-perms", tags: ["permissions"]);
        }

        var (appA, _, providerA) = CreateCache("app-a:prod");
        await using (providerA)
        {
            await appA.RemoveByTagAsync("permissions");
        }

        // A fresh instance of app B (empty L1) must still find its entry in L2.
        var (appBAgain, _, providerBAgain) = CreateCache("app-b:prod");
        await using (providerBAgain)
        {
            var runs = 0;
            var value = await appBAgain.GetOrCreateAsync(
                "perm:u:1",
                0,
                (s, ct) => { Interlocked.Increment(ref runs); return ValueTask.FromResult("reloaded"); },
                tags: ["permissions"]);

            value.ShouldBe("b-perms");
            runs.ShouldBe(0, "another application's tag invalidation must not evict this one's entries");
        }
    }

    [Fact]
    public async Task KeyPrefix_Should_Namespace_The_DataProtection_KeyRing()
    {
        var (_, _, provider) = CreateCache("app-a:prod");
        await using (provider)
        {
            // Protecting forces the key ring to create (and persist) its first key.
            provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Protect("payload");

            using var redis = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
            var db = redis.GetDatabase();
            (await db.KeyExistsAsync("app-a:prod:DataProtection-Keys")).ShouldBeTrue();
            (await db.KeyExistsAsync("DataProtection-Keys")).ShouldBeFalse();
        }
    }

    [ImmutableObject(true)]
    private sealed record TestPayload(string Name, int Number);
}
