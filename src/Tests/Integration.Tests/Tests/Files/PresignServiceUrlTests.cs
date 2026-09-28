using System.Security.Cryptography;
using FSH.Framework.Storage;
using FSH.Framework.Storage.Services;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// Storage:S3:PresignServiceUrl against a real store: the API reaches RustFS as <c>127.0.0.1</c> and hands out
/// presigned URLs for <c>localhost</c>, two host strings for the same container. A 200 on the PUT proves the
/// signature was computed for the public host, not the one the API talks to (SigV4 signs the Host header).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PresignServiceUrlTests
{
    private const string ContentType = "application/octet-stream";
    private readonly FshWebApplicationFactory _factory;

    public PresignServiceUrlTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Happy Path

    [Fact]
    public async Task PresignedUrls_Should_RoundTripBytes_Through_PresignServiceUrl_When_ItDiffersFromServiceUrl()
    {
        // Arrange
        int port = new Uri(_factory.S3ServiceUrl).Port;
        using var provider = BuildProvider($"http://127.0.0.1:{port}", $"http://localhost:{port}");
        var storage = provider.GetRequiredService<IStorageService>();
        string key = $"uploads/presign-endpoint/{Guid.NewGuid():N}.bin";
        byte[] bytes = new byte[1024];
        RandomNumberGenerator.Fill(bytes);

        try
        {
            // Act — presigned PUT straight to the public host
            var upload = await storage.GenerateUploadUrlAsync(key, ContentType, bytes.Length, TimeSpan.FromMinutes(5));
            upload.Url.Host.ShouldBe("localhost");
            upload.Url.Port.ShouldBe(port);

            using var raw = new HttpClient();
            using var body = new ByteArrayContent(bytes);
            body.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
            using var putResp = await raw.PutAsync(upload.Url, body);

            // Assert — the store accepted the signature, the API (on its own host) sees the object, and the
            // presigned GET on the public host serves the same bytes back.
            putResp.StatusCode.ShouldBe(HttpStatusCode.OK);

            var head = await storage.HeadObjectAsync(key);
            head.ShouldNotBeNull();
            head.SizeBytes.ShouldBe(bytes.Length);

            var download = await storage.GenerateDownloadUrlAsync(key, TimeSpan.FromMinutes(5));
            download.Host.ShouldBe("localhost");
            download.Port.ShouldBe(port);

            using var getResp = await raw.GetAsync(download);
            getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await getResp.Content.ReadAsByteArrayAsync()).ShouldBe(bytes);
        }
        finally
        {
            await storage.RemoveAsync(key);
        }

        (await storage.HeadObjectAsync(key)).ShouldBeNull();
    }

    #endregion

    private static ServiceProvider BuildProvider(string serviceUrl, string presignServiceUrl)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "s3",
            ["Storage:S3:Bucket"] = FshWebApplicationFactory.S3Bucket,
            ["Storage:S3:Region"] = "us-east-1",
            ["Storage:S3:ServiceUrl"] = serviceUrl,
            ["Storage:S3:PresignServiceUrl"] = presignServiceUrl,
            ["Storage:S3:AccessKey"] = FshWebApplicationFactory.S3AccessKey,
            ["Storage:S3:SecretKey"] = FshWebApplicationFactory.S3SecretKey,
            ["Storage:S3:ForcePathStyle"] = "true",
            ["Storage:S3:PublicRead"] = "false"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHeroStorage(configuration);
        return services.BuildServiceProvider();
    }
}
