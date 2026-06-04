using System.Security.Cryptography;
using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// Reproduces the 500 reported on POST /api/v1/catalog/products/{id}/images and locks in the
/// expected happy path: upload via Files → attach to product → product reflects the new image.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ProductImagesTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly AuthHelper _auth;

    public ProductImagesTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task AddProductImage_Should_Attach_And_MarkAsThumbnail_When_First_Image()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        // 1. Need a brand and a category. The catalog is seeded — pick the first of each.
        var (brandId, categoryId) = await PickExistingBrandAndCategoryAsync(client);

        // 2. Create a product to attach an image to.
        var productId = await CreateProductAsync(client, brandId, categoryId);

        // 3. Upload + finalize an image via the Files module (presigned flow).
        var (fileAssetId, publicUrl) = await UploadImageAsync(client, productId);

        // 4. Attach the image to the product — this is the call that was returning 500.
        using var attach = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images",
            new { fileAssetId, url = publicUrl });

        if (attach.StatusCode != HttpStatusCode.OK)
        {
            var body = await attach.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"AddProductImage failed: {attach.StatusCode}\n{body}");
        }

        var image = await attach.DeserializeAsync<ProductImageDto>();
        image.ShouldNotBeNull();
        image.FileAssetId.ShouldBe(fileAssetId);
        image.Url.ShouldBe(publicUrl);
        image.IsThumbnail.ShouldBeTrue("first image attached must auto-promote to thumbnail");
        image.SortOrder.ShouldBe(0);

        // 5. The product detail reflects the new image.
        using var getProduct = await client.GetAsync($"{TestConstants.CatalogBasePath}/products/{productId}");
        var product = await getProduct.DeserializeAsync<ProductDto>();
        product.Images.ShouldContain(i => i.Id == image.Id);
        product.ThumbnailUrl.ShouldBe(publicUrl);
    }

    [Fact]
    public async Task AddProductImage_Should_Return404_For_Unknown_Product()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}/images",
            new { fileAssetId = (Guid?)null, url = "https://example.com/image.png" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddProductImage_Should_Return400_When_Url_Is_Empty()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickExistingBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images",
            new { fileAssetId = (Guid?)null, url = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetProductThumbnail_Should_Move_The_Cover_To_The_Second_Image()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickExistingBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        // Attach two images. The first auto-promotes to thumbnail; the second comes in as
        // non-thumbnail. We want to flip the cover to the second.
        var firstId = (await AttachImageAsync(client, productId)).Id;
        var secondId = (await AttachImageAsync(client, productId)).Id;

        // Sanity: state before the flip.
        var before = await GetProductAsync(client, productId);
        before.Images.Single(i => i.Id == firstId).IsThumbnail.ShouldBeTrue();
        before.Images.Single(i => i.Id == secondId).IsThumbnail.ShouldBeFalse();

        // Promote the second image to thumbnail.
        using var promote = await client.PutAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/{secondId}/thumbnail",
            content: null);

        if (promote.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await promote.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"SetProductThumbnail failed: {promote.StatusCode}\n{body}");
        }

        // Verify the flip.
        var after = await GetProductAsync(client, productId);
        after.Images.Single(i => i.Id == secondId).IsThumbnail.ShouldBeTrue("the second image must become the new cover");
        after.Images.Single(i => i.Id == firstId).IsThumbnail.ShouldBeFalse("the previous cover must be demoted");
        after.ThumbnailUrl.ShouldBe(after.Images.Single(i => i.Id == secondId).Url);
    }

    [Fact]
    public async Task SetProductThumbnail_Should_Survive_Repeated_Flips_Across_Three_Images()
    {
        // Regression: a partial UNIQUE index on (ProductId) WHERE IsThumbnail=TRUE was firing
        // whenever EF emitted promote-before-demote, leaving the constraint with two TRUE rows
        // mid-statement. The aggregate enforces the single-thumbnail invariant on its own; the
        // DB-level constraint was belt-and-suspenders that broke for half the EF orderings.
        // Walking the thumbnail through every image, both forward and backward, is the
        // ordering-agnostic way to lock that bug down.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickExistingBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        var ids = new[]
        {
            (await AttachImageAsync(client, productId)).Id,
            (await AttachImageAsync(client, productId)).Id,
            (await AttachImageAsync(client, productId)).Id,
        };

        // Walk the cover through each image, then back to the start. Each step exercises a
        // different demote→promote pair.
        foreach (var nextCoverId in new[] { ids[1], ids[2], ids[0], ids[2], ids[1], ids[0] })
        {
            using var promote = await client.PutAsync(
                $"{TestConstants.CatalogBasePath}/products/{productId}/images/{nextCoverId}/thumbnail",
                content: null);

            if (promote.StatusCode != HttpStatusCode.NoContent)
            {
                var body = await promote.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"SetThumbnail to {nextCoverId} failed: {promote.StatusCode}\n{body}");
            }

            var after = await GetProductAsync(client, productId);
            after.Images.Single(i => i.Id == nextCoverId).IsThumbnail.ShouldBeTrue(
                $"after flipping to {nextCoverId}, it must be the thumbnail");
            after.Images.Count(i => i.IsThumbnail).ShouldBe(1, "exactly one image should be the cover at any time");
        }
    }

    [Fact]
    public async Task SetProductThumbnail_Should_Return404_For_Unknown_Image()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickExistingBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);
        // No image attached — ImageId is unknown.

        using var response = await client.PutAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/{Guid.NewGuid()}/thumbnail",
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a fresh brand id + category id to use as foreign keys when
    /// creating products. The framework no longer auto-seeds catalog data
    /// per tenant (see CatalogDbInitializer notes), so each test creates
    /// its own. Method name retained for grep compatibility, but it now
    /// creates the rows rather than picking pre-existing ones.
    /// </summary>
    private static async Task<(Guid BrandId, Guid CategoryId)> PickExistingBrandAndCategoryAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var brandResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/brands",
            new { name = $"img-brand-{suffix}", description = (string?)null, logoUrl = (string?)null });
        brandResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup helper failed to create brand: {await brandResp.Content.ReadAsStringAsync()}");
        var brandId = await brandResp.DeserializeAsync<Guid>();

        using var categoryResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories",
            new { name = $"img-cat-{suffix}", description = (string?)null, parentCategoryId = (Guid?)null });
        categoryResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup helper failed to create category: {await categoryResp.Content.ReadAsStringAsync()}");
        var categoryId = await categoryResp.DeserializeAsync<Guid>();

        return (brandId, categoryId);
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, Guid brandId, Guid categoryId)
    {
        var sku = "TST-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        using var createResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku,
                name = $"Test product {sku}",
                description = "integration-test product",
                brandId,
                categoryId,
                priceAmount = 19.99m,
                priceCurrency = "USD",
                stock = 10
            });
        if (createResp.StatusCode != HttpStatusCode.OK)
        {
            var body = await createResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"CreateProduct failed: {createResp.StatusCode}\n{body}");
        }
        return await createResp.DeserializeAsync<Guid>();
    }

    private static async Task<ProductImageDto> AttachImageAsync(HttpClient client, Guid productId)
    {
        var (fileAssetId, publicUrl) = await UploadImageAsync(client, productId);
        using var attach = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images",
            new { fileAssetId, url = publicUrl });
        if (attach.StatusCode != HttpStatusCode.OK)
        {
            var body = await attach.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"AttachImage failed: {attach.StatusCode}\n{body}");
        }
        return await attach.DeserializeAsync<ProductImageDto>();
    }

    private static async Task<ProductDto> GetProductAsync(HttpClient client, Guid productId)
    {
        using var resp = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");
        return await resp.DeserializeAsync<ProductDto>();
    }

    private static async Task<(Guid FileAssetId, string PublicUrl)> UploadImageAsync(HttpClient client, Guid productId)
    {
        // upload-url → PUT → finalize, mirroring the Files happy-path tests.
        byte[] bytes = new byte[2048];
        RandomNumberGenerator.Fill(bytes);

        using var presignedResp = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "Product",
            ownerId = productId,
            fileName = "test.png",
            contentType = "image/png",
            sizeBytes = bytes.Length,
            visibility = 0, // Public — so we get a durable BuildPublicUrl back
            category = "Image"
        });
        if (presignedResp.StatusCode != HttpStatusCode.OK)
        {
            var body = await presignedResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"upload-url failed: {presignedResp.StatusCode}\n{body}");
        }
        var presigned = await presignedResp.DeserializeAsync<PresignedUploadResponse>();

        using var raw = new HttpClient();
        using var put = new HttpRequestMessage(HttpMethod.Put, presigned.UploadUrl)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("image/png") }
            }
        };
        using var putResp = await raw.SendAsync(put);
        putResp.EnsureSuccessStatusCode();

        using var finalizeResp = await client.PostAsync($"{FilesBasePath}/{presigned.FileAssetId}/finalize", null);
        finalizeResp.EnsureSuccessStatusCode();

        using var metaResp = await client.GetAsync($"{FilesBasePath}/{presigned.FileAssetId}");
        var meta = await metaResp.DeserializeAsync<FileAssetDto>();
        meta.PublicUrl.ShouldNotBeNullOrWhiteSpace("Public files should return a durable publicUrl from GetFileMetadata");

        return (presigned.FileAssetId, meta.PublicUrl!);
    }

    // Lightweight projection — the test only needs Id off the category row. Setter is required
    // for JSON deserialization; suppress the "unused setter" analyzer false-positive.
#pragma warning disable S1144
    private sealed class CategoryRow
    {
        public Guid Id { get; set; }
    }
#pragma warning restore S1144
}
