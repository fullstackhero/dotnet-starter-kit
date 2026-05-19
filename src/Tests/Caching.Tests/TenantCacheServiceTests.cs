using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Caching.Tests;

/// <summary>
/// Verifies that <see cref="TenantHybridCache"/> automatically scopes every cache
/// key and tag with the current tenant's identifier, preventing cross-tenant data leaks.
/// </summary>
public sealed class TenantCacheServiceTests
{
    private static AppTenantInfo BuildTenant(string id) =>
        new(id, id, string.Empty, $"{id}@test.com", issuer: null);

    private static IMultiTenantContextAccessor BuildAccessor(AppTenantInfo tenant)
    {
        var accessor = Substitute.For<IMultiTenantContextAccessor>();
        accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(tenant));
        return accessor;
    }

    private static (ITenantCacheService sut, HybridCache innerCache) CreateSut(string tenantId)
    {
        var tenant = BuildTenant(tenantId);
        var accessor = BuildAccessor(tenant);

        var services = new ServiceCollection();
        services.AddHeroCaching(new ConfigurationBuilder().Build());
        var provider = services.BuildServiceProvider();
        var innerCache = provider.GetRequiredService<HybridCache>();

        return (TenantHybridCache.Create(innerCache, accessor), innerCache);
    }

    [Fact]
    public async Task GetOrCreateAsync_DifferentTenants_ShouldNotShareEntries()
    {
        // Arrange — two caches sharing the same underlying HybridCache but with different tenants
        var services = new ServiceCollection();
        services.AddHeroCaching(new ConfigurationBuilder().Build());
        var provider = services.BuildServiceProvider();
        var innerCache = provider.GetRequiredService<HybridCache>();

        var sutA = TenantHybridCache.Create(innerCache, BuildAccessor(BuildTenant("tenant-a")));
        var sutB = TenantHybridCache.Create(innerCache, BuildAccessor(BuildTenant("tenant-b")));

        int factoryCallCount = 0;

        // Act — write through tenant A's cache
        await sutA.GetOrCreateAsync("shared-key", "state", (_, _) =>
        {
            factoryCallCount++;
            return ValueTask.FromResult("value-for-a");
        });

        // Act — read through tenant B's cache (same logical key, different tenant scope)
        var resultForB = await sutB.GetOrCreateAsync("shared-key", "state", (_, _) =>
        {
            factoryCallCount++;
            return ValueTask.FromResult("value-for-b");
        });

        // Assert — factory must be called TWICE: tenant isolation is working
        factoryCallCount.ShouldBe(2);
        resultForB.ShouldBe("value-for-b");
    }

    [Fact]
    public async Task SetAsync_RemoveAsync_Should_ScopeKey_To_Tenant()
    {
        // Arrange
        var (sut, _) = CreateSut("tenant-set");
        await sut.SetAsync("mykey", "myvalue");

        // Act — remove the tenant-scoped entry
        await sut.RemoveAsync("mykey");

        // Re-read — factory should run again after eviction
        int factoryCount = 0;
        await sut.GetOrCreateAsync("mykey", "state", (_, _) =>
        {
            factoryCount++;
            return ValueTask.FromResult("fresh");
        });

        // Assert — factory ran, confirming removal operated on the correct scoped key
        factoryCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldThrow_When_TenantContextIsNull()
    {
        // Arrange — accessor returns null context (no tenant resolved)
        var accessor = Substitute.For<IMultiTenantContextAccessor>();
        accessor.MultiTenantContext.Returns((IMultiTenantContext?)null!);

        var services = new ServiceCollection();
        services.AddHeroCaching(new ConfigurationBuilder().Build());
        var innerCache = services.BuildServiceProvider().GetRequiredService<HybridCache>();

        var sut = TenantHybridCache.Create(innerCache, accessor);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.GetOrCreateAsync("key", "state",
                (_, _) => ValueTask.FromResult("v")).AsTask());
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldThrow_When_TenantIdIsEmpty()
    {
        // Arrange — tenant context present but TenantId is empty
        var tenant = BuildTenant(string.Empty);
        var accessor = BuildAccessor(tenant);

        var services = new ServiceCollection();
        services.AddHeroCaching(new ConfigurationBuilder().Build());
        var innerCache = services.BuildServiceProvider().GetRequiredService<HybridCache>();

        var sut = TenantHybridCache.Create(innerCache, accessor);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.GetOrCreateAsync("key", "state",
                (_, _) => ValueTask.FromResult("v")).AsTask());
    }

    /// <summary>
    /// Regression test for the tag-format mismatch bug: entries tagged with a logical
    /// tag (e.g. "themes") must be evictable via <see cref="ITenantCacheService.RemoveByTagAsync(string, CancellationToken)"/>
    /// using the same logical tag string. Without the <c>t:{tenantId}:</c> prefix applied
    /// consistently on both SET and REMOVE paths, this round-trip silently fails.
    /// </summary>
    [Fact]
    public async Task RemoveByTagAsync_RoundTrip_Should_EvictTaggedEntry()
    {
        // Arrange
        var (sut, _) = CreateSut("tenant-tag-rt");
        int factoryCallCount = 0;

        // Prime the cache — entry is stored with tag "my-tag"
        await sut.GetOrCreateAsync(
            "tagged-key",
            "state",
            (_, _) =>
            {
                factoryCallCount++;
                return ValueTask.FromResult("original-value");
            },
            tags: ["my-tag"]);

        factoryCallCount.ShouldBe(1, "factory should have run once to prime the cache");

        // Act — evict by the same logical tag string
        await sut.RemoveByTagAsync("my-tag");

        // Re-read: entry must be gone, so factory runs again
        var result = await sut.GetOrCreateAsync(
            "tagged-key",
            "state",
            (_, _) =>
            {
                factoryCallCount++;
                return ValueTask.FromResult("refreshed-value");
            },
            tags: ["my-tag"]);

        // Assert
        factoryCallCount.ShouldBe(2, "factory must run a second time — RemoveByTagAsync should have evicted the entry");
        result.ShouldBe("refreshed-value");
    }

    /// <summary>
    /// Same round-trip guarantee for the multi-tag overload.
    /// </summary>
    [Fact]
    public async Task RemoveByTagAsync_MultiTag_RoundTrip_Should_EvictTaggedEntry()
    {
        // Arrange
        var (sut, _) = CreateSut("tenant-multitag-rt");
        int factoryCallCount = 0;

        await sut.GetOrCreateAsync(
            "multi-tagged-key",
            "state",
            (_, _) =>
            {
                factoryCallCount++;
                return ValueTask.FromResult("initial");
            },
            tags: ["tag-a", "tag-b"]);

        factoryCallCount.ShouldBe(1);

        // Act — evict via the multi-tag overload using one of the stored tags
        await sut.RemoveByTagAsync(["tag-a", "tag-b"]);

        // Assert — must miss
        await sut.GetOrCreateAsync(
            "multi-tagged-key",
            "state",
            (_, _) =>
            {
                factoryCallCount++;
                return ValueTask.FromResult("re-fetched");
            });

        factoryCallCount.ShouldBe(2, "multi-tag removal must evict the entry");
    }
}
