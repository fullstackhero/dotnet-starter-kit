using FluentAssertions;
using FSH.WebApi.Application.Common.Caching;
using Xunit;

namespace Infrastructure.Test.Caching;

public abstract class CacheServiceTests
{
    private record TestRecord(Guid Id, string StringValue, DateTime DateTimeValue);

    private const string _testKey = "testkey";
    private const string _testValue = "testvalue";

    private readonly ICacheService _sut;

    protected CacheServiceTests(ICacheService cacheService) => _sut = cacheService;

    [Fact]
    public void ThrowsGivenNullKey()
    {
        var action = () => { string? result = _sut.Get<string>(null!); };

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReturnsNullGivenNonExistingKey()
    {
        string? result = _sut.Get<string>(_testKey);

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
        _sut.Set(testKey, testValue);
        T? result = _sut.Get<T>(testKey);

        result.Should().Be(testValue);
    }

    [Fact]
    public void ReturnsExistingObjectGivenExistingKey()
    {
        var expected = new TestRecord(Guid.NewGuid(), _testValue, DateTime.UtcNow);

        _sut.Set(_testKey, expected);
        var result = _sut.Get<TestRecord>(_testKey);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ReturnsNullGivenAnExpiredKey()
    {
        _sut.Set(_testKey, _testValue, TimeSpan.FromMilliseconds(200));

        string? result = _sut.Get<string>(_testKey);
        Assert.Equal(_testValue, result);

        await Task.Delay(250);
        result = _sut.Get<string>(_testKey);

        result.Should().BeNull();
    }
}