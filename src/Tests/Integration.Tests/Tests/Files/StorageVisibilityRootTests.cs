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
using Microsoft.Extensions.Options;

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
        (await DownloadViaPresignedUrlAsync(client, id)).ShouldBe(bytes);

        // Act + Assert — and back again.
        using (var toPrivate = await client.PatchAsJsonAsync($"{FilesBasePath}/{id}/visibility", new { visibility = 1 }))
        {
            toPrivate.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        (await ReadStorageKeyAsync(TestConstants.RootTenantId, id)).ShouldBe(privateKey);
        (await ObjectExistsAsync(privateKey)).ShouldBeTrue();
        (await ObjectExistsAsync(publicKey)).ShouldBeFalse();
        (await DownloadViaPresignedUrlAsync(client, id)).ShouldBe(bytes);
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

        // …and the public URLs other modules persisted for them, built with a base browsers can't reach
        // (what compose stored before it set PublicBaseUrl). The move must repair the base, not keep it.
        const string StaleBase = "http://rustfs:9000/" + Bucket + "/";
        var originalAvatarUrl = await SetAvatarUrlAsync(adminId, StaleBase + avatar.Key);
        var productImageId = await SeedProductImageAsync(productImage.Id, StaleBase + productImage.Key);

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

            // Persisted URLs followed their objects, onto the API's current public URL.
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

    [Fact]
    public async Task MigrationJob_Should_MovePastFilesThatKeepFailing_And_StillMigrateTheFilesAfterThem()
    {
        // Arrange — more permanently failing rows (objects missing) than fit in one page, then good rows
        // after them (UUIDv7 ids sort by creation time, so these come later in id order).
        var broken = new List<LegacyAsset>();
        for (var i = 0; i < 3; i++)
        {
            broken.Add(await SeedLegacyAssetAsync(TestConstants.RootTenantId, "MyFiles", null, Visibility.Public, withObject: false));
        }

        var good1 = await SeedLegacyAssetAsync(TestConstants.RootTenantId, "MyFiles", null, Visibility.Public);
        var good2 = await SeedLegacyAssetAsync(TestConstants.RootTenantId, "MyFiles", null, Visibility.Public);

        try
        {
            // Act — a page of 2 is smaller than the number of broken rows. The run may report the failures.
            _ = await Record.ExceptionAsync(() => RunMigrationJobAsync(batchSize: 2));

            // Assert — the broken rows did not stop the rows after them.
            (await ReadStorageKeyAsync(TestConstants.RootTenantId, good1.Id)).ShouldBe("public/" + good1.Key);
            (await ReadStorageKeyAsync(TestConstants.RootTenantId, good2.Id)).ShouldBe("public/" + good2.Key);
            foreach (var asset in broken)
            {
                (await ReadStorageKeyAsync(TestConstants.RootTenantId, asset.Id))
                    .ShouldBe(asset.Key, "a row whose object is missing is never repointed");
            }
        }
        finally
        {
            // Permanently broken rows would make every later run in this DB report failures.
            await DeleteRowsAsync(TestConstants.RootTenantId, broken.Select(b => b.Id));
        }
    }

    [Fact]
    public async Task MigrationJob_Should_SkipAFile_WhoseVisibilityChangedWhileTheJobWaitedForIt()
    {
        // Arrange — a legacy public file, then another writer holding its row (as a visibility flip would).
        var asset = await SeedLegacyAssetAsync(TestConstants.RootTenantId, "MyFiles", null, Visibility.Public);
        var connectionString = _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["DatabaseOptions:ConnectionString"];

        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var lockRow = new Npgsql.NpgsqlCommand(
            """SELECT 1 FROM files."FileAssets" WHERE "Id" = @id FOR UPDATE""", connection, transaction))
        {
            lockRow.Parameters.AddWithValue("id", asset.Id);
            await lockRow.ExecuteScalarAsync();
        }

        // Act — the job starts while the row is held; the other writer makes the file private and commits.
        var run = RunMigrationJobAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
        await using (var makePrivate = new Npgsql.NpgsqlCommand(
            """UPDATE files."FileAssets" SET "Visibility" = 1 WHERE "Id" = @id""", connection, transaction))
        {
            makePrivate.Parameters.AddWithValue("id", asset.Id);
            await makePrivate.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        _ = await Record.ExceptionAsync(() => run);

        // Assert — the job re-read the row after the other writer and left the now-private file alone.
        (await ReadVisibilityAsync(TestConstants.RootTenantId, asset.Id)).ShouldBe(Visibility.Private);
        (await ReadStorageKeyAsync(TestConstants.RootTenantId, asset.Id)).ShouldBe(asset.Key);
        (await ReadObjectAsync(asset.Key)).ShouldBe(asset.Bytes);
        (await ObjectExistsAsync("public/" + asset.Key)).ShouldBeFalse("a private file must not be copied under public/");
    }

    [Fact]
    public async Task MakingAFilePrivate_Should_StillRemoveThePublicObject_When_TheFirstDeleteSilentlyFails()
    {
        // Arrange — a public upload, readable anonymously through the public/* policy.
        await ApplyPublicPrefixPolicyAsync();
        using var client = await _auth.CreateRootAdminClientAsync();
        var bytes = RandomBytes(256);
        var id = await UploadAndFinalizeAsync(client, "going-private.pdf", bytes, visibility: 0);
        var publicKey = await ReadStorageKeyAsync(TestConstants.RootTenantId, id);

        // Act — make it private through a store whose first delete does nothing (S3StorageService.RemoveAsync
        // swallows store errors), then let the outbox deliver what was committed with the move.
        using (var scope = _factory.Services.CreateScope())
        {
            SetTenant(scope, TestConstants.RootTenantId);
            var flaky = new DeleteFailsOnceStorage(scope.ServiceProvider.GetRequiredService<IStorageService>());
            var relocator = ActivatorUtilities.CreateInstance<FSH.Modules.Files.Services.FileStorageRelocator>(scope.ServiceProvider, flaky);
            await relocator.ApplyAsync(id, asset =>
            {
                asset.ChangeVisibility(Visibility.Private);
                return true;
            });
            flaky.SwallowedDeletes.ShouldBe(1);
        }

        await OutboxDrain.DrainAsync(_factory.Services);

        // Assert — the row moved, the bytes are intact, and the public copy is gone for anonymous readers.
        (await ReadStorageKeyAsync(TestConstants.RootTenantId, id)).ShouldStartWith("private/");
        (await DownloadViaPresignedUrlAsync(client, id)).ShouldBe(bytes);
        (await ObjectExistsAsync(publicKey)).ShouldBeFalse("the retried delete must remove the old public object");
        using var anonymous = new HttpClient();
        using var response = await anonymous.GetAsync(new Uri($"{_factory.S3ServiceUrl}/{Bucket}/{publicKey}"));
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Finalize_Should_MoveAnUploadPresignedBeforeTheUpgrade_UnderThePublicRoot()
    {
        // Arrange — a public upload whose presigned PUT (and so its key) predates visibility roots:
        // the row is still PendingUpload at a tenants/… key and the browser has already PUT the bytes.
        using var client = await _auth.CreateRootAdminClientAsync();
        var adminId = await GetUserIdAsync(TestConstants.RootTenantId, TestConstants.RootAdminEmail);
        var id = Guid.CreateVersion7();
        var legacyKey = $"tenants/{TestConstants.RootTenantId}/myfiles/2025/01/{id:N}/in-flight.pdf";
        var bytes = RandomBytes(300);

        var s3 = _factory.Services.GetRequiredService<IAmazonS3>();
        using (var stream = new MemoryStream(bytes))
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = legacyKey,
                InputStream = stream,
                ContentType = "application/pdf",
            });
        }

        using (var scope = _factory.Services.CreateScope())
        {
            SetTenant(scope, TestConstants.RootTenantId);
            var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
            db.FileAssets.Add(FileAsset.CreatePending(
                id, "MyFiles", null, "in-flight.pdf", "in-flight.pdf", "application/pdf", bytes.Length, legacyKey,
                Visibility.Public, createdByUserId: adminId, uploadDeadline: DateTimeOffset.UtcNow.AddMinutes(15)));
            await db.SaveChangesAsync();
        }

        // Act
        using var finalize = await client.PostAsync($"{FilesBasePath}/{id}/finalize", null);

        // Assert — finalized straight onto the public root, not left for a later migration run.
        finalize.StatusCode.ShouldBe(HttpStatusCode.OK);
        var key = await ReadStorageKeyAsync(TestConstants.RootTenantId, id);
        key.ShouldBe("public/" + legacyKey);
        (await ReadObjectAsync(key)).ShouldBe(bytes);
        (await ObjectExistsAsync(legacyKey)).ShouldBeFalse();
    }

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

    private async Task RunMigrationJobAsync(int batchSize = 100)
    {
        // A fresh, tenant-less scope — the shape Hangfire's activator gives the enqueued job.
        using var scope = _factory.Services.CreateScope();
        var options = Options.Create(new FSH.Modules.Files.FilesOptions { LegacyKeyMigrationBatchSize = batchSize });
        var job = ActivatorUtilities.CreateInstance<MigrateLegacyPublicFileKeysJob>(scope.ServiceProvider, options);
        await job.RunAsync(CancellationToken.None);
    }

    /// <summary>
    /// A store whose first delete silently does nothing — what <c>S3StorageService.RemoveAsync</c> does when
    /// the store errors, since it logs and swallows.
    /// </summary>
    private sealed class DeleteFailsOnceStorage(IStorageService inner) : IStorageService
    {
        public int SwallowedDeletes { get; private set; }

        public Task RemoveAsync(string path, CancellationToken cancellationToken = default)
        {
            if (SwallowedDeletes == 0)
            {
                SwallowedDeletes++;
                return Task.CompletedTask;
            }

            return inner.RemoveAsync(path, cancellationToken);
        }

        public Task<string> UploadAsync<T>(FSH.Framework.Shared.Storage.FileUploadRequest request, FSH.Framework.Storage.FileType fileType, CancellationToken cancellationToken = default)
            where T : class => inner.UploadAsync<T>(request, fileType, cancellationToken);
        public Task<FSH.Framework.Storage.DTOs.FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default) => inner.DownloadAsync(path, cancellationToken);
        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) => inner.ExistsAsync(path, cancellationToken);
        public Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default) => inner.GetSizeAsync(path, cancellationToken);
        public Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default) => inner.CopyAsync(sourceKey, destinationKey, cancellationToken);
        public Task<FSH.Framework.Shared.Storage.PresignedUploadUrl> GenerateUploadUrlAsync(string storageKey, string contentType, long maxBytes, TimeSpan ttl, CancellationToken cancellationToken = default)
            => inner.GenerateUploadUrlAsync(storageKey, contentType, maxBytes, ttl, cancellationToken);
        public Task<Uri> GenerateDownloadUrlAsync(string storageKey, TimeSpan ttl, string? responseContentDisposition = null, CancellationToken cancellationToken = default)
            => inner.GenerateDownloadUrlAsync(storageKey, ttl, responseContentDisposition, cancellationToken);
        public Task<FSH.Framework.Shared.Storage.StoredObjectMetadata?> HeadObjectAsync(string storageKey, CancellationToken cancellationToken = default) => inner.HeadObjectAsync(storageKey, cancellationToken);
        public string BuildPublicUrl(string storageKey) => inner.BuildPublicUrl(storageKey);
    }

    private async Task DeleteRowsAsync(string tenantId, IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, tenantId);
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        await db.FileAssets.IgnoreQueryFilters().Where(f => idList.Contains(f.Id)).ExecuteDeleteAsync();
    }

    private async Task<Visibility> ReadVisibilityAsync(string tenantId, Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenant(scope, tenantId);
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        return await db.FileAssets.IgnoreQueryFilters().AsNoTracking().Where(f => f.Id == id).Select(f => f.Visibility).SingleAsync();
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

    private async Task<LegacyAsset> SeedLegacyAssetAsync(
        string tenantId, string ownerType, Guid? ownerId, Visibility visibility, bool withObject = true)
    {
        var id = Guid.CreateVersion7();
#pragma warning disable CA1308 // storage path segments are lower-case
        var key = $"tenants/{tenantId}/{ownerType.ToLowerInvariant()}/2025/01/{id:N}/legacy.png";
#pragma warning restore CA1308
        var bytes = RandomBytes(512);

        var s3 = _factory.Services.GetRequiredService<IAmazonS3>();
        if (withObject)
        {
            using var stream = new MemoryStream(bytes);
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

    private static async Task<byte[]> DownloadViaPresignedUrlAsync(HttpClient client, Guid id)
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
