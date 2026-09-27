using FSH.Framework.Storage;
using FSH.Framework.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework.Tests.Storage;

/// <summary>
/// Presigned URLs are handed to browsers, so they must point at <c>Storage:S3:PresignServiceUrl</c> when the
/// API itself reaches the store on an internal address (compose: <c>http://rustfs:9000</c>). SigV4 presigning
/// is offline, so these run through the real <c>AddHeroStorage</c> wiring without a store to talk to.
/// </summary>
public sealed class S3PresignEndpointTests
{
    private const string InternalServiceUrl = "http://internal-store:9000";
    private const string PublicPresignServiceUrl = "https://public.example.test";
    private const string StorageKey = "uploads/probe/file.png";

    #region Happy Path

    [Fact]
    public async Task GenerateUploadUrlAsync_Should_TargetPresignServiceUrl_When_PresignServiceUrlIsSet()
    {
        // Arrange
        using var provider = BuildProvider(InternalServiceUrl, PublicPresignServiceUrl);
        var storage = provider.GetRequiredService<IStorageService>();

        // Act
        var presigned = await storage.GenerateUploadUrlAsync(StorageKey, "image/png", 1024, TimeSpan.FromMinutes(5));

        // Assert
        presigned.Url.Scheme.ShouldBe(Uri.UriSchemeHttps);
        presigned.Url.Host.ShouldBe("public.example.test");
        presigned.Url.Port.ShouldBe(443);
    }

    [Fact]
    public async Task GenerateDownloadUrlAsync_Should_TargetPresignServiceUrl_When_PresignServiceUrlIsSet()
    {
        // Arrange
        using var provider = BuildProvider(InternalServiceUrl, PublicPresignServiceUrl);
        var storage = provider.GetRequiredService<IStorageService>();

        // Act
        var url = await storage.GenerateDownloadUrlAsync(StorageKey, TimeSpan.FromMinutes(5));

        // Assert
        url.Scheme.ShouldBe(Uri.UriSchemeHttps);
        url.Host.ShouldBe("public.example.test");
        url.Port.ShouldBe(443);
    }

    // The protocol has to come from PresignServiceUrl, not ServiceUrl: an http-only internal store behind a TLS
    // edge needs https URLs, and an https internal store with a plain-http public endpoint needs http ones.
    [Theory]
    [InlineData("http://internal-store:9000", "https://public.example.test:8443", "https", 8443)]
    [InlineData("https://internal-store:9443", "http://public.example.test:9000", "http", 9000)]
    public async Task GeneratePresignedUrls_Should_FollowPresignServiceUrlScheme_When_ItDiffersFromServiceUrl(
        string serviceUrl,
        string presignServiceUrl,
        string expectedScheme,
        int expectedPort)
    {
        // Arrange
        using var provider = BuildProvider(serviceUrl, presignServiceUrl);
        var storage = provider.GetRequiredService<IStorageService>();

        // Act
        var upload = await storage.GenerateUploadUrlAsync(StorageKey, "image/png", 1024, TimeSpan.FromMinutes(5));
        var download = await storage.GenerateDownloadUrlAsync(StorageKey, TimeSpan.FromMinutes(5));

        // Assert
        foreach (var url in new[] { upload.Url, download })
        {
            url.Scheme.ShouldBe(expectedScheme);
            url.Host.ShouldBe("public.example.test");
            url.Port.ShouldBe(expectedPort);
        }
    }

    [Fact]
    public void AddHeroStorage_Should_PassStartupValidation_When_PresignServiceUrlIsAbsoluteHttpUrl()
    {
        // Arrange
        using var provider = BuildProvider(InternalServiceUrl, PublicPresignServiceUrl);

        // Act
        provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert — sanity: the rule below rejects bad values without rejecting a good one.
        provider.GetRequiredService<IOptions<FSH.Framework.Storage.S3.S3StorageOptions>>().Value
            .PresignServiceUrl.ShouldBe(PublicPresignServiceUrl);
    }

    #endregion

    #region Exception

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("/relative/path")]
    [InlineData("ftp://public.example.test")]
    [InlineData("https://public.example.test/s3")]
    [InlineData("https://public.example.test/?region=x")]
    public void AddHeroStorage_Should_FailAtStartup_When_PresignServiceUrlIsNotAbsoluteHttpUrl(string presignServiceUrl)
    {
        // Arrange
        using var provider = BuildProvider(InternalServiceUrl, presignServiceUrl);

        // Act — through IStartupValidator, which is what .ValidateOnStart() registers; resolving
        // IOptions<>.Value would only fail on the first presign, long after boot.
        var act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        act.ShouldThrow<OptionsValidationException>()
            .Failures.ShouldContain(failure => failure.Contains("PresignServiceUrl", StringComparison.Ordinal));
    }

    #endregion

    #region Edge Cases

    // Regression guard: without PresignServiceUrl the URLs are exactly what they were before the option existed.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GeneratePresignedUrls_Should_TargetServiceUrl_When_PresignServiceUrlIsEmpty(string? presignServiceUrl)
    {
        // Arrange
        using var provider = BuildProvider(InternalServiceUrl, presignServiceUrl);
        var storage = provider.GetRequiredService<IStorageService>();

        // Act
        var upload = await storage.GenerateUploadUrlAsync(StorageKey, "image/png", 1024, TimeSpan.FromMinutes(5));
        var download = await storage.GenerateDownloadUrlAsync(StorageKey, TimeSpan.FromMinutes(5));

        // Assert
        foreach (var url in new[] { upload.Url, download })
        {
            url.Scheme.ShouldBe(Uri.UriSchemeHttp);
            url.Host.ShouldBe("internal-store");
            url.Port.ShouldBe(9000);
        }
    }

    // A quoted or padded FSH_S3_PUBLIC_URL passes Uri.TryCreate, so it must not reach the SDK untrimmed and
    // fail every upload long after startup validation said it was fine.
    [Fact]
    public async Task GenerateUploadUrlAsync_Should_TargetTrimmedPresignServiceUrl_When_ValueIsPadded()
    {
        // Arrange
        using var provider = BuildProvider(InternalServiceUrl, "  https://public.example.test  ");
        provider.GetRequiredService<IStartupValidator>().Validate();
        var storage = provider.GetRequiredService<IStorageService>();

        // Act
        var presigned = await storage.GenerateUploadUrlAsync(StorageKey, "image/png", 1024, TimeSpan.FromMinutes(5));

        // Assert
        presigned.Url.Scheme.ShouldBe(Uri.UriSchemeHttps);
        presigned.Url.Host.ShouldBe("public.example.test");
    }

    #endregion

    private static ServiceProvider BuildProvider(string serviceUrl, string? presignServiceUrl)
    {
        // Explicit keys keep the SDK off the ambient credential chain, which would reach for the network.
        var settings = new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "s3",
            ["Storage:S3:Bucket"] = "presign-tests",
            ["Storage:S3:Region"] = "us-east-1",
            ["Storage:S3:ServiceUrl"] = serviceUrl,
            ["Storage:S3:AccessKey"] = "test-access-key",
            ["Storage:S3:SecretKey"] = "test-secret-key",
            ["Storage:S3:ForcePathStyle"] = "true",
            ["Storage:S3:PresignServiceUrl"] = presignServiceUrl
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHeroStorage(configuration);
        return services.BuildServiceProvider();
    }
}
