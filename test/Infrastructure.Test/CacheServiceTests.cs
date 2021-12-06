using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Infrastructure.Caching;
using DN.WebApi.Infrastructure.Common.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Test;

public abstract class CacheServiceTests<TCacheService>
    where TCacheService : ICacheService
{
    private static string _testKey = "testkey";
    private static string _testValue = "testvalue";
    protected abstract TCacheService CreateCacheService();

    [Fact]
    public void GettingANonExistingValueReturnsNull()
    {
        var sut = CreateCacheService();
        string? test = sut.Get<string>(_testKey);
        Assert.Null(test);
    }

    [Fact]
    public void GettingAnExistingValueReturnsThatValue()
    {
        var sut = CreateCacheService();
        sut.Set(_testKey, _testValue);
        string? actual = sut.Get<string>(_testKey);
        Assert.Equal(_testValue, actual);
    }

    [Fact]
    public async Task GettingAnExpiredValueReturnsNull()
    {
        var sut = CreateCacheService();
        sut.Set(_testKey, _testValue, TimeSpan.FromMilliseconds(100));
        string? actual = sut.Get<string>(_testKey);
        Assert.Equal(_testValue, actual);
        await Task.Delay(100);
        actual = sut.Get<string>(_testKey);
        Assert.Null(actual);
    }

    [Fact]
    public void GettingAnExistingObjectReturnsThatObject()
    {
        var expected = new TestRecord(Guid.NewGuid(), _testValue, DateTime.UtcNow);
        var sut = CreateCacheService();
        sut.Set(_testKey, expected);
        var actual = sut.Get<TestRecord>(_testKey);
        Assert.Equal(expected, actual);
    }
}

internal record TestRecord(Guid id, string stringValue, DateTime dateTimeValue);

public class LocalCacheServiceTests : CacheServiceTests<LocalCacheService>
{
    protected override LocalCacheService CreateCacheService() =>
        new LocalCacheService(new MemoryCache(new MemoryCacheOptions()), NullLogger<LocalCacheService>.Instance);
}

public class DistributedCacheServiceTests : CacheServiceTests<DistributedCacheService>
{
    protected override DistributedCacheService CreateCacheService() =>
        new DistributedCacheService(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), new NewtonSoftService(), NullLogger<DistributedCacheService>.Instance);
}
