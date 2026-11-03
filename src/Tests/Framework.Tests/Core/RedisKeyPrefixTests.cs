using FSH.Framework.Core.Common;

namespace Framework.Tests.Core;

public sealed class RedisKeyPrefixTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(":", "")]
    [InlineData("MyApp", "MyApp:")]
    [InlineData("MyApp:prod", "MyApp:prod:")]
    [InlineData("MyApp:prod:", "MyApp:prod:")]
    [InlineData("  MyApp:prod::  ", "MyApp:prod:")]
    public void Normalize_Should_YieldEmptyOrExactlyOneTrailingColon(string? configured, string expected)
    {
        RedisKeyPrefix.Normalize(configured).ShouldBe(expected);
    }
}
