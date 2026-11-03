using FSH.Framework.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework.Tests.Storage;

/// <summary>
/// #1423: keys are <c>{Prefix}/public/…</c> and <c>{Prefix}/private/…</c>. A prefix whose first segment is
/// itself <c>public</c> puts every private key under <c>public/</c>, where the anonymous-read policy applies,
/// so such a prefix has to fail at startup rather than quietly expose private files.
/// </summary>
public sealed class S3PrefixValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tenant-files")]
    [InlineData("apps/fsh")]
    [InlineData("publicity")]
    [InlineData("files/public")]
    public void AddHeroStorage_Should_PassStartupValidation_When_PrefixDoesNotStartWithAVisibilityRoot(string? prefix)
    {
        // Arrange
        using var provider = BuildProvider(prefix);

        // Act
        provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        provider.GetRequiredService<IOptions<FSH.Framework.Storage.S3.S3StorageOptions>>().Value.Prefix.ShouldBe(prefix);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("Public")]
    [InlineData("PRIVATE/")]
    [InlineData("public/apps")]
    [InlineData("/private/apps")]
    [InlineData("  public  ")]
    public void AddHeroStorage_Should_FailAtStartup_When_PrefixStartsWithAVisibilityRoot(string prefix)
    {
        // Arrange
        using var provider = BuildProvider(prefix);

        // Act
        var act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        act.ShouldThrow<OptionsValidationException>()
            .Failures.ShouldContain(failure => failure.Contains("Storage:S3:Prefix", StringComparison.Ordinal));
    }

    // The rule belongs to the S3 provider; a host on local storage (the DbMigrator, dev) never sees it.
    [Fact]
    public void AddHeroStorage_Should_NotValidatePrefix_When_ProviderIsLocal()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "local",
            ["Storage:S3:Prefix"] = "public",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHeroStorage(configuration);
        using var provider = services.BuildServiceProvider();

        // Act — local storage registers no S3 options validation, so there may be no validator at all.
        var act = () => provider.GetService<IStartupValidator>()?.Validate();

        // Assert
        act.ShouldNotThrow();
    }

    private static ServiceProvider BuildProvider(string? prefix)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "s3",
            ["Storage:S3:Bucket"] = "prefix-tests",
            ["Storage:S3:Region"] = "us-east-1",
            ["Storage:S3:ServiceUrl"] = "http://internal-store:9000",
            ["Storage:S3:AccessKey"] = "test-access-key",
            ["Storage:S3:SecretKey"] = "test-secret-key",
            ["Storage:S3:Prefix"] = prefix,
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHeroStorage(configuration);
        return services.BuildServiceProvider();
    }
}
