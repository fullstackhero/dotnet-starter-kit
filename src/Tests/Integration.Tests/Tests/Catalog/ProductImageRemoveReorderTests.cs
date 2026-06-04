using System.Security.Cryptography;
using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// Coverage for the product-image remove + reorder endpoints. The attach / thumbnail flows live in
/// <see cref="ProductImagesTests"/>; this file only exercises
/// <c>DELETE /products/{id}/images/{imageId}</c> and <c>PUT /products/{id}/images/order</c>,
/// including the validator and 404 paths.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ProductImageRemoveReorderTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ProductImageRemoveReorderTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── remove: happy path ──────────────────────────────────────────

    [Fact]
    public async Task RemoveProductImage_Should_Detach_And_PromoteNextThumbnail_When_RemovingCover()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        // First image auto-promotes to thumbnail; second comes in non-thumbnail.
        var firstId = (await AttachImageAsync(client, productId)).Id;
        var secondId = (await AttachImageAsync(client, productId)).Id;

        using var remove = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/{firstId}");
        remove.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetProductAsync(client, productId);
        after.Images.ShouldNotContain(i => i.Id == firstId, "removed image must be gone");
        after.Images.Single(i => i.Id == secondId).IsThumbnail.ShouldBeTrue(
            "removing the cover must promote the lowest-sorted remaining image");
        after.ThumbnailUrl.ShouldBe(after.Images.Single(i => i.Id == secondId).Url);
    }

    [Fact]
    public async Task RemoveProductImage_Should_Detach_NonThumbnail_And_Leave_Cover_Intact()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        var coverId = (await AttachImageAsync(client, productId)).Id;
        var extraId = (await AttachImageAsync(client, productId)).Id;

        using var remove = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/{extraId}");
        remove.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetProductAsync(client, productId);
        after.Images.ShouldNotContain(i => i.Id == extraId);
        after.Images.Single(i => i.Id == coverId).IsThumbnail.ShouldBeTrue(
            "removing a non-cover image must not disturb the existing thumbnail");
    }

    [Fact]
    public async Task RemoveProductImage_Should_Leave_Product_Without_Cover_When_LastImageRemoved()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        var onlyId = (await AttachImageAsync(client, productId)).Id;

        using var remove = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/{onlyId}");
        remove.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetProductAsync(client, productId);
        after.Images.ShouldBeEmpty();
        after.ThumbnailUrl.ShouldBeNull("a product with no images has no cover");
    }

    // ─── remove: 404 / validation ────────────────────────────────────

    [Fact]
    public async Task RemoveProductImage_Should_Return404_For_Unknown_Product()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}/images/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveProductImage_Should_Return404_For_Unknown_Image_On_Existing_Product()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);
        // No image attached — handler translates the domain "not found" into a 404.

        using var response = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── remove: auth gating ─────────────────────────────────────────

    [Fact]
    public async Task RemoveProductImage_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}/images/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── reorder: happy path ─────────────────────────────────────────

    [Fact]
    public async Task ReorderProductImages_Should_Apply_Supplied_Order()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        var a = (await AttachImageAsync(client, productId)).Id;
        var b = (await AttachImageAsync(client, productId)).Id;
        var c = (await AttachImageAsync(client, productId)).Id;

        // Reverse the natural 0,1,2 order to c,b,a.
        using var reorder = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/order",
            new { orderedImageIds = new[] { c, b, a } });
        reorder.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetProductAsync(client, productId);
        after.Images.Single(i => i.Id == c).SortOrder.ShouldBe(0);
        after.Images.Single(i => i.Id == b).SortOrder.ShouldBe(1);
        after.Images.Single(i => i.Id == a).SortOrder.ShouldBe(2);
    }

    [Fact]
    public async Task ReorderProductImages_Should_Append_Unlisted_Images_After_The_Listed_Ones()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        var a = (await AttachImageAsync(client, productId)).Id;
        var b = (await AttachImageAsync(client, productId)).Id;
        var c = (await AttachImageAsync(client, productId)).Id;

        // Only list the last image; a and b are unlisted and must be appended after it,
        // preserving their relative order.
        using var reorder = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/order",
            new { orderedImageIds = new[] { c } });
        reorder.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetProductAsync(client, productId);
        after.Images.Single(i => i.Id == c).SortOrder.ShouldBe(0);
        // a and b keep their relative order (a was attached before b) after the listed image.
        after.Images.Single(i => i.Id == a).SortOrder.ShouldBe(1);
        after.Images.Single(i => i.Id == b).SortOrder.ShouldBe(2);
    }

    [Fact]
    public async Task ReorderProductImages_Should_Ignore_Unknown_Image_Ids()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        var a = (await AttachImageAsync(client, productId)).Id;
        var b = (await AttachImageAsync(client, productId)).Id;

        // A stray id that doesn't belong to the product is silently skipped by the aggregate.
        using var reorder = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/order",
            new { orderedImageIds = new[] { b, Guid.NewGuid(), a } });
        reorder.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetProductAsync(client, productId);
        after.Images.Single(i => i.Id == b).SortOrder.ShouldBe(0);
        after.Images.Single(i => i.Id == a).SortOrder.ShouldBe(1);
    }

    [Fact]
    public async Task ReorderProductImages_Should_Succeed_With_Empty_List()
    {
        // Empty list is valid (NotNull, not NotEmpty) — it's a no-op that leaves existing order intact.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        var a = (await AttachImageAsync(client, productId)).Id;
        var b = (await AttachImageAsync(client, productId)).Id;

        using var reorder = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/order",
            new { orderedImageIds = Array.Empty<Guid>() });
        reorder.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetProductAsync(client, productId);
        after.Images.Single(i => i.Id == a).SortOrder.ShouldBe(0);
        after.Images.Single(i => i.Id == b).SortOrder.ShouldBe(1);
    }

    // ─── reorder: 404 / validation ───────────────────────────────────

    [Fact]
    public async Task ReorderProductImages_Should_Return404_For_Unknown_Product()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}/images/order",
            new { orderedImageIds = new[] { Guid.NewGuid() } });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderProductImages_Should_Return400_When_OrderedImageIds_Is_Null()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        // Body present but the required collection is null — validator (NotNull) rejects it.
        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/images/order",
            new { orderedImageIds = (Guid[]?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderProductImages_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}/images/order",
            new { orderedImageIds = new[] { Guid.NewGuid() } });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<(Guid BrandId, Guid CategoryId)> CreateBrandAndCategoryAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var brandResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/brands",
            new { name = $"rr-brand-{suffix}", description = (string?)null, logoUrl = (string?)null });
        brandResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup helper failed to create brand: {await brandResp.Content.ReadAsStringAsync()}");
        var brandId = await brandResp.DeserializeAsync<Guid>();

        using var categoryResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories",
            new { name = $"rr-cat-{suffix}", description = (string?)null, parentCategoryId = (Guid?)null });
        categoryResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup helper failed to create category: {await categoryResp.Content.ReadAsStringAsync()}");
        var categoryId = await categoryResp.DeserializeAsync<Guid>();

        return (brandId, categoryId);
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, Guid brandId, Guid categoryId)
    {
        var sku = "RR-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        using var createResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku,
                name = $"Reorder product {sku}",
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
        using var resp = await client.GetAsync($"{TestConstants.CatalogBasePath}/products/{productId}");
        return await resp.DeserializeAsync<ProductDto>();
    }

    private static async Task<(Guid FileAssetId, string PublicUrl)> UploadImageAsync(HttpClient client, Guid productId)
    {
        byte[] bytes = new byte[2048];
        RandomNumberGenerator.Fill(bytes);

        using var presignedResp = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "Product",
            ownerId = productId,
            fileName = "test.png",
            contentType = "image/png",
            sizeBytes = bytes.Length,
            visibility = 0, // Public
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
        meta.PublicUrl.ShouldNotBeNullOrWhiteSpace();

        return (presigned.FileAssetId, meta.PublicUrl!);
    }
}
