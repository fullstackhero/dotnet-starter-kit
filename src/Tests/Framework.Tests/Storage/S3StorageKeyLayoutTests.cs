using Amazon.S3;
using Amazon.S3.Model;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;
using FSH.Framework.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Tests.Storage;

/// <summary>
/// Bucket policies grant anonymous read on <c>public/*</c> only, so where <c>S3StorageService</c> puts
/// objects decides whether they can be read. These run the real <c>AddHeroStorage</c> wiring against a
/// substituted <see cref="IAmazonS3"/> and assert on the requests it receives.
/// </summary>
public sealed class S3StorageKeyLayoutTests
{
    private const string Bucket = "layout-tests";

    private sealed class Probe { }

    [Fact]
    public async Task UploadAsync_Should_PutTheObjectUnderThePublicRoot()
    {
        // Arrange — UploadAsync always returns a durable public URL, so its objects must be anonymously readable.
        var s3 = Substitute.For<IAmazonS3>();
        using var provider = BuildProvider(s3);
        var storage = provider.GetRequiredService<IStorageService>();

        // Act
        var url = await storage.UploadAsync<Probe>(
            new FileUploadRequest { FileName = "avatar.png", ContentType = "image/png", Data = [1, 2, 3] },
            FileType.Image);

        // Assert
        await s3.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(r => r.BucketName == Bucket && r.Key.StartsWith("public/uploads/probe/", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        url.ShouldContain("/public/uploads/probe/");
    }

    [Fact]
    public async Task CopyAsync_Should_IssueAServerSideCopyWithinTheBucket()
    {
        // Arrange
        var s3 = Substitute.For<IAmazonS3>();
        using var provider = BuildProvider(s3);
        var storage = provider.GetRequiredService<IStorageService>();

        // Act
        await storage.CopyAsync("tenants/t/myfiles/x.png", "public/tenants/t/myfiles/x.png");

        // Assert
        await s3.Received(1).CopyObjectAsync(
            Arg.Is<CopyObjectRequest>(r =>
                r.SourceBucket == Bucket && r.SourceKey == "tenants/t/myfiles/x.png"
                && r.DestinationBucket == Bucket && r.DestinationKey == "public/tenants/t/myfiles/x.png"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CopyAsync_Should_Throw_When_TheStoreRejectsTheCopy()
    {
        // Arrange — a move deletes the source after the copy, so a failed copy must surface.
        var s3 = Substitute.For<IAmazonS3>();
        s3.CopyObjectAsync(Arg.Any<CopyObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns<CopyObjectResponse>(_ => throw new AmazonS3Exception("NoSuchKey"));
        using var provider = BuildProvider(s3);
        var storage = provider.GetRequiredService<IStorageService>();

        // Act + Assert
        await Should.ThrowAsync<AmazonS3Exception>(() => storage.CopyAsync("missing.png", "public/missing.png"));
    }

    [Fact]
    public async Task CopyAsync_Should_ChargeTheCopiedBytes_When_QuotasAreEnabled_And_ATenantIsResolved()
    {
        // Arrange — the copy is charged and the delete that follows it is refunded, so a move is net-zero.
        var s3 = Substitute.For<IAmazonS3>();
        s3.GetObjectMetadataAsync(Arg.Any<GetObjectMetadataRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetObjectMetadataResponse { ContentLength = 42 });
        var quotas = Substitute.For<IQuotaService>();
        using var provider = BuildProvider(s3, quotas, tenantId: "tenant-a");
        var storage = provider.GetRequiredService<IStorageService>();

        // Act
        await storage.CopyAsync("tenants/tenant-a/x.png", "public/tenants/tenant-a/x.png");
        await storage.RemoveAsync("tenants/tenant-a/x.png");

        // Assert
        await quotas.Received(1).RecordAsync("tenant-a", QuotaResource.StorageBytes, 42, Arg.Any<CancellationToken>());
        await quotas.Received(1).RecordAsync("tenant-a", QuotaResource.StorageBytes, -42, Arg.Any<CancellationToken>());
    }

    private static ServiceProvider BuildProvider(IAmazonS3 s3, IQuotaService? quotas = null, string? tenantId = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "s3",
            ["Storage:S3:Bucket"] = Bucket,
            ["Storage:S3:Region"] = "us-east-1",
            ["Storage:S3:ServiceUrl"] = "http://store.test:9000",
            ["Storage:S3:AccessKey"] = "test-access-key",
            ["Storage:S3:SecretKey"] = "test-secret-key",
            ["Storage:S3:ForcePathStyle"] = "true",
            ["QuotaOptions:Enabled"] = quotas is null ? "false" : "true",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHeroStorage(configuration);

        // Last registration wins for a single-service resolve.
        services.AddSingleton(s3);

        if (quotas is not null)
        {
            services.AddSingleton(quotas);
            var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
            if (tenantId is not null)
            {
                accessor.MultiTenantContext.Returns(
                    new MultiTenantContext<AppTenantInfo>(new AppTenantInfo(tenantId, tenantId)));
            }
            services.AddSingleton(accessor);
        }

        return services.BuildServiceProvider();
    }
}
