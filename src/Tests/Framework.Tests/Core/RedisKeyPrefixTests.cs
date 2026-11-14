using FSH.Framework.Core.Common;

namespace Framework.Tests.Core;

public sealed class RedisKeyPrefixTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(":", "")]
    [InlineData("MyApp", "MyApp:")]
    [InlineData("MyApp:prod", "MyApp:prod:")]
    [InlineData("MyApp:prod:", "MyApp:prod:")]
    [InlineData("MyApp:prod::", "MyApp:prod:")]
    [InlineData("My.App-1_x:prod", "My.App-1_x:prod:")]
    public void Normalize_Should_YieldEmptyOrExactlyOneTrailingColon(string? configured, string expected)
    {
        RedisKeyPrefix.Normalize(configured).ShouldBe(expected);
    }

    [Theory]
    [InlineData("{MyApp}")]       // Redis Cluster hash tag: every key would land in one slot
    [InlineData("MyApp:{prod")]
    [InlineData("MyApp}")]
    [InlineData("   ")]
    [InlineData(" MyApp")]
    [InlineData("MyApp ")]
    [InlineData("My App")]
    [InlineData("MyApp\t")]
    [InlineData("MyApp\n")]
    public void Normalize_Should_Reject_HashTagBracesAndWhitespace(string configured)
    {
        var ex = Should.Throw<InvalidOperationException>(() => RedisKeyPrefix.Normalize(configured));
        ex.Message.ShouldContain(RedisKeyPrefix.ConfigurationKey);
    }
}
