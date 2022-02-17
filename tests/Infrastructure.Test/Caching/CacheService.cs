using FluentAssertions;
using FSH.WebApi.Application.Common.Caching;
using Xunit;

namespace Infrastructure.Test.Caching;

public abstract class CacheService<TCacheService>
    where TCacheService : ICacheService
{
    private record TestRecord(Guid Id, string StringValue, DateTime DateTimeValue);

    private const string _testKey = "testkey";
    private const string _testValue = "testvalue";

    protected abstract TCacheService CreateCacheService();

    [Fact]
    public void ThrowsGivenNullKey()
    {
        var sut = CreateCacheService();

        var action = () => { string? result = sut.Get<string>(null!); };

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReturnsNullGivenNonExistingKey()
    {
        var sut = CreateCacheService();

        string? result = sut.Get<string>(_testKey);

        result.Should().BeNull();
    }

#pragma warning disable RCS1158
    public static IEnumerable<object[]> ValueData => new List<object[]>
#pragma warning restore RCS1158
        {
            new object[] { _testKey, _testValue },
            new object[] { "integer", 1 },
            new object[] { "long", 1L },
            new object[] { "double", 1.0 },
            new object[] { "bool", true },
            new object[] { "date", new DateTime(2022, 1, 1) },
        };

    [Theory]
    [MemberData(nameof(ValueData))]
    public void ReturnsExistingValueGivenExistingKey<T>(string testKey, T testValue)
    {
        var sut = CreateCacheService();

        sut.Set(testKey, testValue);
        T? result = sut.Get<T>(testKey);

        result.Should().Be(testValue);
    }

    [Fact]
    public void ReturnsExistingObjectGivenExistingKey()
    {
        var expected = new TestRecord(Guid.NewGuid(), _testValue, DateTime.UtcNow);
        var sut = CreateCacheService();

        sut.Set(_testKey, expected);
        var result = sut.Get<TestRecord>(_testKey);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ReturnsNullGivenAnExpiredKey()
    {
        var sut = CreateCacheService();
        sut.Set(_testKey, _testValue, TimeSpan.FromMilliseconds(200));

        string? result = sut.Get<string>(_testKey);
        Assert.Equal(_testValue, result);

        await Task.Delay(250);
        result = sut.Get<string>(_testKey);

        result.Should().BeNull();
    }
}