using FSH.Framework.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.Tests;

/// <summary>
/// End-to-end behavior tests for HybridCache using the in-memory distributed cache backend
/// wired through <see cref="Extensions.AddHeroCaching"/>. Covers the core GetOrCreate / Set /
/// Remove / RemoveByTag paths consumers rely on.
/// </summary>
public sealed class HybridCacheBehaviorTests
{
    private static HybridCache CreateCache()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddHeroCaching(config);
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_InvokeFactory_Once_When_KeyIsMissing()
    {
        // Arrange
        var cache = CreateCache();
        var calls = 0;

        // Act — two sequential calls with the same key
        var first = await cache.GetOrCreateAsync(
            "test:single",
            ct =>
            {
                Interlocked.Increment(ref calls);
                return ValueTask.FromResult("value-1");
            }).ConfigureAwait(true);

        var second = await cache.GetOrCreateAsync(
            "test:single",
            ct =>
            {
                Interlocked.Increment(ref calls);
                return ValueTask.FromResult("value-2");
            }).ConfigureAwait(true);

        // Assert — factory invoked exactly once, cached value returned on second call
        first.ShouldBe("value-1");
        second.ShouldBe("value-1");
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveAsync_Should_EvictEntry()
    {
        // Arrange
        var cache = CreateCache();
        await cache.SetAsync("test:remove", "cached").ConfigureAwait(true);

        // Act
        await cache.RemoveAsync("test:remove").ConfigureAwait(true);
        var calls = 0;
        var result = await cache.GetOrCreateAsync(
            "test:remove",
            ct =>
            {
                Interlocked.Increment(ref calls);
                return ValueTask.FromResult("fresh");
            }).ConfigureAwait(true);

        // Assert — factory re-ran after removal
        result.ShouldBe("fresh");
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveByTagAsync_Should_InvalidateAllEntriesWithTag()
    {
        // Arrange
        var cache = CreateCache();
        var tags = new[] { "group-a" };

        await cache.SetAsync("test:tagged:1", "v1", tags: tags).ConfigureAwait(true);
        await cache.SetAsync("test:tagged:2", "v2", tags: tags).ConfigureAwait(true);
        await cache.SetAsync("test:untagged", "v3").ConfigureAwait(true);

        // Act
        await cache.RemoveByTagAsync("group-a").ConfigureAwait(true);

        // Assert — both tagged entries re-execute factory, untagged entry still cached
        var taggedRuns = 0;
        var untaggedRuns = 0;

        await cache.GetOrCreateAsync(
            "test:tagged:1",
            ct => { Interlocked.Increment(ref taggedRuns); return ValueTask.FromResult("reloaded-1"); }).ConfigureAwait(true);
        await cache.GetOrCreateAsync(
            "test:tagged:2",
            ct => { Interlocked.Increment(ref taggedRuns); return ValueTask.FromResult("reloaded-2"); }).ConfigureAwait(true);
        await cache.GetOrCreateAsync(
            "test:untagged",
            ct => { Interlocked.Increment(ref untaggedRuns); return ValueTask.FromResult("should-not-run"); }).ConfigureAwait(true);

        taggedRuns.ShouldBe(2);
        untaggedRuns.ShouldBe(0);
    }

    [Fact]
    public async Task SetAsync_Then_GetOrCreate_Should_ReturnStoredValue_Without_InvokingFactory()
    {
        // Arrange
        var cache = CreateCache();
        var payload = new TestPayload("hello", 42);
        await cache.SetAsync("test:set", payload).ConfigureAwait(true);

        var calls = 0;

        // Act
        var fetched = await cache.GetOrCreateAsync(
            "test:set",
            ct =>
            {
                Interlocked.Increment(ref calls);
                return ValueTask.FromResult(new TestPayload("wrong", 0));
            }).ConfigureAwait(true);

        // Assert
        fetched.ShouldBe(payload);
        calls.ShouldBe(0);
    }

    private sealed record TestPayload(string Name, int Value);
}
