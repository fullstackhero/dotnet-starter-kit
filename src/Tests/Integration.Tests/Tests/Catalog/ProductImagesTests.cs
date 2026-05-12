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

    // ── helpers ────────────────────────────────────────────────────────────

    private static async Task<(Guid BrandId, Guid CategoryId)> PickExistingBrandAndCategoryAsync(HttpClient client)
    {
        using var brandsResp = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/brands?pageNumber=1&pageSize=1");
        var brands = await brandsResp.DeserializeAsync<PagedResult<Infrastructure.BrandDto>>();
        brands.Items.Count.ShouldBeGreaterThan(0, "seed data should provide at least one brand");

        using var categoriesResp = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/categories?pageNumber=1&pageSize=1");
        var categories = await categoriesResp.DeserializeAsync<PagedResult<CategoryRow>>();
        categories.Items.Count.ShouldBeGreaterThan(0, "seed data should provide at least one category");

        return (brands.Items[0].Id, categories.Items[0].Id);
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
