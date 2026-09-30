using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Domain;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Jobs;
using FSH.Modules.Identity.Data;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// #1410: visibility lives in the storage key. New uploads land under <c>public/</c> or <c>private/</c>,
/// a visibility flip moves the object, bucket policies grant anonymous read on <c>public/*</c> only,
/// and <see cref="MigrateLegacyPublicFileKeysJob"/> moves public files uploaded before the change (and
/// the URLs other modules persisted for them) so nothing breaks on upgrade. Runs against real RustFS.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class StorageVisibilityRootTests
{
    private const string FilesBasePath = "/api/v1/files";
    private const string Bucket = FshWebApplicationFactory.S3Bucket;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public StorageVisibilityRootTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region New uploads

    [Theory]
    [InlineData(0, "public/")]
    [InlineData(1, "private/")]
    public async Task Upload_Should_StoreTheObjectUnderTheRootForItsVisibility(int visibility, string expectedRoot)
    {
        // Arrange + Act
        using var client = await _auth.CreateRootAdminClientAsync();
        var bytes = RandomBytes(256);
        var id = await UploadAndFinalizeAsync(client, "rooted.pdf", bytes, visibility);

        // Assert — the row's key carries the root and the bytes are really stored there.
        var key = await ReadStorageKeyAsync(TestConstants.RootTenantId, id);
        key.ShouldStartWith($"{expectedRoot}tenants/{TestConstants.RootTenantId}/myfiles/");
        (await ObjectExistsAsync(key)).ShouldBeTrue();
    }

    [Fact]
    public async Task Upload_Should_HandOutAPublicUrlUnderThePublicRoot_When_FileIsPublic()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var id = await UploadAndFinalizeAsync(client, "public-url.pdf", RandomBytes(256), visibility: 0);

        using var response = await client.GetAsync($"{FilesBasePath}/{id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.DeserializeAsync<FileAssetDto>();

        dto.PublicUrl.ShouldNotBeNull();
        dto.PublicUrl.ShouldContain($"/{Bucket}/public/tenants/");
    }

    #endregion

    #region Visibility change

    [Fact]
    public async Task ChangeVisibility_Should_MoveTheObjectBetweenRoots_And_KeepTheBytes()
    {
        // Arrange — a private upload.
        using var client = await _auth.CreateRootAdminClientAsync();
        var bytes = RandomBytes(1024);
        var id = await UploadAndFinalizeAsync(client, "flip.pdf", bytes, visibility: 1);
        var privateKey = await ReadStorageKeyAsync(TestConstants.RootTenantId, id);
        privateKey.ShouldStartWith("private/");

        // Act — make it public.
        using (var toPublic = await client.PatchAsJsonAsync($"{FilesBasePath}/{id}/visibility", new { visibility = 0 }))
        {
            toPublic.StatusCode.ShouldBe(HttpStatusCode.OK);
            var dto = await toPublic.DeserializeAsync<FileAssetDto>();
            dto.PublicUrl.ShouldNotBeNull();
            dto.PublicUrl.ShouldContain("/public/tenants/");
        }

        // Assert — row and object moved under public/, the private copy is gone, bytes intact.
        var publicKey = await ReadStorageKeyAsync(TestConstants.RootTenantId, id);
        publicKey.ShouldBe("public/" + privateKey["private/".Length..]);
        (await ObjectExistsAsync(publicKey)).ShouldBeTrue();
        (await ObjectExistsAsync(privateKey)).ShouldBeFalse("the old object is deleted once the row points at the new one");
        (await DownloadAsync(client, id)).ShouldBe(bytes);

        // Act + Assert — and back again.
        using (var toPrivate = await client.PatchAsJsonAsync($"{FilesBasePath}/{id}/visibility", new { visibility = 1 }))
        {
            toPrivate.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        (await ReadStorageKeyAsync(TestConstants.RootTenantId, id)).ShouldBe(privateKey);
        (await ObjectExistsAsync(privateKey)).ShouldBeTrue();
        (await ObjectExistsAsync(publicKey)).ShouldBeFalse();
        (await DownloadAsync(client, id)).ShouldBe(bytes);
    }

    #endregion

    #region Bucket policy

    [Fact]
    public async Task AnonymousGet_Should_ServePublicObjects_And_RefusePrivateOnes_UnderThePublicPrefixPolicy()
    {
        // Arrange — the policy every stack now applies: anonymous s3:GetObject on public/* only.
        await ApplyPublicPrefixPolicyAsync();
        using var client = await _auth.CreateRootAdminClientAsync();
        var publicId = await UploadAndFinalizeAsync(client, "anon-public.pdf", RandomBytes(128), visibility: 0);
        var privateId = await UploadAndFinalizeAsync(client, "anon-private.pdf", RandomBytes(128), visibility: 1);
        var publicKey = await ReadStorageKeyAsync(TestConstants.RootTenantId, publicId);
        var privateKey = await ReadStorageKeyAsync(TestConstants.RootTenantId, privateId);

        // Act — plain unsigned GETs, as a browser following a public URL makes them.
        using var anonymous = new HttpClient();
        using var publicResponse = await anonymous.GetAsync(new Uri($"{_factory.S3ServiceUrl}/{Bucket}/{publicKey}"));
        using var privateResponse = await anonymous.GetAsync(new Uri($"{_factory.S3ServiceUrl}/{Bucket}/{privateKey}"));

        // Assert
        publicResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        privateResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Legacy public-key migration

    [Fact]
    public async Task MigrationJob_Should_MoveLegacyPublicFiles_InEveryTenant_RewriteTheirUrls_And_BeIdempotent()
    {
        // Arrange — a second tenant, so the job has to walk tenants rather than use the ambient one.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var otherTenantId = $"vis-root-{Guid.NewGuid():N}"[..20];
        await CreateTenantAsync(rootClient, otherTenantId, $"admin@{otherTenantId}.com");
        await WaitForProvisioningAsync(rootClient, otherTenantId);

        var adminId = await GetUserIdAsync(TestConstants.RootTenantId, TestConstants.RootAdminEmail);

        // Files uploaded before #1410: keys under tenants/…, objects at those keys.
        var avatar = await SeedLegacyAssetAsync(TestConstants.RootTenantId, "User", Guid.Parse(adminId), Visibility.Public);
        var productImage = await SeedLegacyAssetAsync(TestConstants.RootTenantId, "Product", null, Visibility.Public);
        var legacyPrivate = await SeedLegacyAssetAsync(TestConstants.RootTenantId, "MyFiles", null, Visibility.Private);
        var otherTenantPublic = await SeedLegacyAssetAsync(otherTenantId, "MyFiles", null, Visibility.Public);

        // …and the public URLs other modules persisted for them.
        var originalAvatarUrl = await SetAvatarUrlAsync(adminId, PublicUrlFor(avatar.Key));
        var productImageId = await SeedProductImageAsync(productImage.Id, PublicUrlFor(productImage.Key));

        try
        {
            // Act
            await RunMigrationJobAsync();
            await OutboxDrain.DrainAsync(_factory.Services);

            // Assert — public files moved under public/, old objects gone, bytes intact.
            foreach (var (tenantId, asset) in new[]
            {
                (TestConstants.RootTenantId, avatar),
                (TestConstants.RootTenantId, productImage),
                (otherTenantId, otherTenantPublic),
            })
            {
                var newKey = await ReadStorageKeyAsync(tenantId, asset.Id);
                newKey.ShouldBe("public/" + asset.Key, $"legacy public file in tenant {tenantId} must move under public/");
                (await ReadObjectAsync(newKey)).ShouldBe(asset.Bytes);
                (await ObjectExistsAsync(asset.Key)).ShouldBeFalse("the legacy object is deleted after the move");
            }

            // Private legacy files stay where they are: no policy grants anonymous read on them.
            (await ReadStorageKeyAsync(TestConstants.RootTenantId, legacyPrivate.Id)).ShouldBe(legacyPrivate.Key);
            (await ObjectExistsAsync(legacyPrivate.Key)).ShouldBeTrue();

            // Persisted URLs followed their objects.
            (await ReadAvatarUrlAsync(adminId)).ShouldBe(PublicUrlFor("public/" + avatar.Key));
            (await ReadProductImageUrlAsync(productImageId)).ShouldBe(PublicUrlFor("public/" + productImage.Key));

            // Act — a second run (every API start enqueues one) finds nothing left to do.
            await RunMigrationJobAsync();
            await OutboxDrain.DrainAsync(_factory.Services);

            // Assert — nothing moved again, nothing was lost.
            (await ReadStorageKeyAsync(TestConstants.RootTenantId, avatar.Id)).ShouldBe("public/" + avatar.Key);
            (await ReadObjectAsync("public/" + avatar.Key)).ShouldBe(avatar.Bytes);
            (await ReadStorageKeyAsync(TestConstants.RootTenantId, legacyPrivate.Id)).ShouldBe(legacyPrivate.Key);
            (await ReadAvatarUrlAsync(adminId)).ShouldBe(PublicUrlFor("public/" + avatar.Key));
        }
        finally
        {
            await SetAvatarUrlAsync(adminId, originalAvatarUrl);
        }
    }

    #endregion

    // ─── helpers ─────────────────────────────────────────────────────

    private sealed record LegacyAsset(Guid Id, string Key, byte[] Bytes);

    private static byte[] RandomBytes(int size)
    {
        byte[] bytes = new byte[size];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    // Public URLs are built from the S3 endpoint + bucket in the test host (no PublicBaseUrl).
    private string PublicUrlFor(string key) => $"{_factory.S3ServiceUrl}/{Bucket}/{key}";

    private async Task RunMigrationJobAsync()
    {
        // A fresh, tenant-less scope — the shape Hangfire's activator gives the enqueued job.
        using var scope = _factory.Services.CreateScope();
        var job = ActivatorUtilities.CreateInstance<MigrateLegacyPublicFileKeysJob>(scope.ServiceProvider);
        await job.RunAsync(CancellationToken.None);
    }

    private async Task ApplyPublicPrefixPolicyAsync()
    {
        var s3 = _factory.Services.GetRequiredService<IAmazonS3>();
        await s3.PutBucketPolicyAsync(new PutBucketPolicyRequest
        {
            BucketName = Bucket,
            Policy = $$"""
                {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{Bucket}}/public/*"]}]}
                """,
        });
    }

    private async Task<LegacyAsset> SeedLegacyAssetAsync(string tenantId, string ownerType, Guid? ownerId, Visibility visibility)
    {
        var id = Guid.CreateVersion7();
#pragma warning disable CA1308 // storage path segments are lower-case
        var key = $"tenants/{tenantId}/{ownerType.ToLowerInvariant()}/2025/01/{id:N}/legacy.png";
#pragma warning restore CA1308
        var bytes = RandomBytes(512);

        var s3 = _factory.Services.GetRequiredService<IAmazonS3>();
        using (var stream = new MemoryStream(bytes))
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = key,
                InputStream = stream,
                ContentType = "image/png",
            });
        }

        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, tenantId);
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        var asset = FileAsset.CreatePending(
            id, ownerType, ownerId, "legacy.png", "legacy.png", "image/png", bytes.Length, key, visibility,
            createdByUserId: Guid.NewGuid().ToString(), uploadDeadline: DateTimeOffset.UtcNow.AddMinutes(15));
        asset.MarkAvailable(bytes.Length, ScanStatus.Clean);
        db.FileAssets.Add(asset);
        await db.SaveChangesAsync();

        return new LegacyAsset(id, key, bytes);
    }

    private async Task<Guid> SeedProductImageAsync(Guid fileAssetId, string url)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, TestConstants.RootTenantId);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var brand = Brand.Create($"vis-brand-{suffix}", null, null);
        var category = Category.Create($"vis-category-{suffix}", null, null);
        var product = Product.Create($"VIS-{suffix}", $"vis product {suffix}", null, brand.Id, category.Id, new Money(10m, "USD"), 1);
        var image = product.AddImage(fileAssetId, url);
        db.Brands.Add(brand);
        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return image.Id;
    }

    private async Task<string> ReadProductImageUrlAsync(Guid imageId)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, TestConstants.RootTenantId);
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await db.Set<ProductImage>().AsNoTracking().Where(i => i.Id == imageId).Select(i => i.Url).SingleAsync();
    }

    private async Task<string> GetUserIdAsync(string tenantId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, tenantId);
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Users.AsNoTracking().Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
    }

    /// <summary>Sets the root admin's avatar URL and returns the previous one.</summary>
    private async Task<string?> SetAvatarUrlAsync(string userId, string? url)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, TestConstants.RootTenantId);
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == userId);
        var previous = user.ImageUrl?.ToString();
        user.ImageUrl = url is null ? null : new Uri(url, UriKind.RelativeOrAbsolute);
        await db.SaveChangesAsync();
        return previous;
    }

    private async Task<string?> ReadAvatarUrlAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, TestConstants.RootTenantId);
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        return user.ImageUrl?.ToString();
    }

    private async Task<string> ReadStorageKeyAsync(string tenantId, Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, tenantId);
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        var key = await db.FileAssets.IgnoreQueryFilters().AsNoTracking()
            .Where(f => f.Id == id).Select(f => f.StorageKey).FirstOrDefaultAsync();
        key.ShouldNotBeNull();
        return key;
    }

    private async Task<bool> ObjectExistsAsync(string key)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IStorageService>().ExistsAsync(key);
    }

    private async Task<byte[]> ReadObjectAsync(string key)
    {
        var s3 = _factory.Services.GetRequiredService<IAmazonS3>();
        using var response = await s3.GetObjectAsync(Bucket, key);
        using var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    // Tenant context is an AsyncLocal: a value set inside an async helper does not flow back to the caller,
    // so this stays synchronous and is called in the same method that resolves the DbContext.
    private static void SetTenant(IServiceScope scope, string tenantId)
    {
        var tenant = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(tenantId).GetAwaiter().GetResult();
        tenant.ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
    }

    private static async Task<byte[]> DownloadAsync(HttpClient client, Guid id)
    {
        using var urlResp = await client.GetAsync($"{FilesBasePath}/{id}/url");
        urlResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var download = await urlResp.DeserializeAsync<PresignedDownloadResponse>();

        using var raw = new HttpClient();
        using var getResp = await raw.GetAsync(download.Url);
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await getResp.Content.ReadAsByteArrayAsync();
    }

    private static async Task<Guid> UploadAndFinalizeAsync(HttpClient client, string fileName, byte[] bytes, int visibility)
    {
        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "MyFiles",
            ownerId = (Guid?)null,
            fileName,
            contentType = "application/pdf",
            sizeBytes = bytes.Length,
            visibility,
            category = "Document",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var presigned = await response.DeserializeAsync<PresignedUploadResponse>();

        using var raw = new HttpClient();
        using var put = new HttpRequestMessage(HttpMethod.Put, presigned.UploadUrl)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/pdf") }
            }
        };
        using var putResp = await raw.SendAsync(put);
        putResp.EnsureSuccessStatusCode();

        using var finalize = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);
        finalize.EnsureSuccessStatusCode();
        return presigned.FileAssetId;
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    // Only the overall Status is trusted: finished steps read "Completed" while later ones still run.
    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId)
    {
        const int maxRetries = 60;
        for (int i = 0; i < maxRetries; i++)
        {
            using var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var status = await statusResponse.Content.ReadFromJsonAsync<TenantProvisioningStatusDto>();
                if (string.Equals(status?.Status, "Completed", StringComparison.Ordinal))
                {
                    return;
                }

                if (string.Equals(status?.Status, "Failed", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed at {status?.CurrentStep}: {status?.Error}");
                }
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds.");
    }
}
