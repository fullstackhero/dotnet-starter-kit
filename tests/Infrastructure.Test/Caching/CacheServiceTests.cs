using DN.WebApi.Application.Common.Interfaces;
using FluentAssertions;
using Xunit;

namespace Infrastructure.Test.Caching;

public abstract class CacheServiceTests<TCacheService>
    where TCacheService : ICacheService
{
    private record TestRecord(Guid Id, string StringValue, DateTime DateTimeValue);

    private const string _testKey = "testkey";
    private const string _testValue = "testvalue";

    protected abstract TCacheService CreateCacheService();

    [Fact]
    public void GettingANonExistingValueReturnsNull()
    {
        var sut = CreateCacheService();
        string? test = sut.Get<string>(_testKey);
        Assert.Null(test);
    }

    /// <summary>
    /// Sample Test Case using Fluent Assertions.
    /// </summary>
    /// <param name="testKey"></param>
    /// <param name="testValue"></param>
    /// <param name="expectedCacheValue"></param>
    [Theory]
    [InlineData("testKey", "testValue", "testValue")]
    [InlineData("someKey", "helloWorld", "helloWorld")]
    public void GettingAnExistingValueReturnsThatValue(string testKey, string testValue, string expectedCacheValue)
    {
        var sut = CreateCacheService();
        sut.Set(testKey, testValue);
        string? result = sut.Get<string>(testKey);
        result.Should().Be(expectedCacheValue);
    }

    [Fact]
    public async Task GettingAnExpiredValueReturnsNull()
    {
        var sut = CreateCacheService();
        sut.Set(_testKey, _testValue, TimeSpan.FromMilliseconds(200));
        string? actual = sut.Get<string>(_testKey);
        Assert.Equal(_testValue, actual);
        await Task.Delay(200);
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