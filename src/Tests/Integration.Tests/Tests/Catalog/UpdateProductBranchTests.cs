using FSH.Modules.Catalog.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// Targets the branch-heavy validation paths in <c>UpdateProductCommandHandler</c> that the
/// happy-path update test in <see cref="ProductsEndpointTests"/> never hits: changing the brand /
/// category to a non-existent id (each guarded only when the FK actually changes), and renaming a
/// product into another live product's slug (the post-Update unique-slug conflict check).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class UpdateProductBranchTests
{
    private readonly AuthHelper _auth;

    public UpdateProductBranchTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task UpdateProduct_Should_Return404_When_BrandId_Changed_To_Unknown()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}",
            new
            {
                productId,
                name = UniqueName("BrandSwap"),
                description = (string?)null,
                brandId = Guid.NewGuid(), // changed to a brand that doesn't exist
                categoryId,
                isActive = true,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_Should_Return404_When_CategoryId_Changed_To_Unknown()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}",
            new
            {
                productId,
                name = UniqueName("CatSwap"),
                description = (string?)null,
                brandId, // unchanged → brand branch is skipped
                categoryId = Guid.NewGuid(), // changed to a category that doesn't exist
                isActive = true,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_Should_Move_Product_To_Different_Existing_Brand_And_Category()
    {
        // Exercises the "FK changed AND target exists" branch for both brand and category — the
        // AnyAsync check passes and the update goes through.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var (otherBrandId, otherCategoryId) = await CreateBrandAndCategoryAsync(client);
        var productId = await CreateProductAsync(client, brandId, categoryId);

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}",
            new
            {
                productId,
                name = UniqueName("Moved"),
                description = "moved to a different brand + category",
                brandId = otherBrandId,
                categoryId = otherCategoryId,
                isActive = true,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var getResp = await client.GetAsync($"{TestConstants.CatalogBasePath}/products/{productId}");
        var updated = await getResp.DeserializeAsync<ProductDto>();
        updated.BrandId.ShouldBe(otherBrandId);
        updated.CategoryId.ShouldBe(otherCategoryId);
    }

    [Fact]
    public async Task UpdateProduct_Should_Return409_When_Rename_Collides_With_Another_Products_Slug()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);

        // Two distinct products. We'll rename the second one onto the first one's name → same slug.
        var existingName = UniqueName("Taken");
        await CreateProductAsync(client, brandId, categoryId, name: existingName);
        var secondId = await CreateProductAsync(client, brandId, categoryId, name: UniqueName("Other"));

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{secondId}",
            new
            {
                productId = secondId,
                name = existingName, // collides on the derived slug with the first product
                description = (string?)null,
                brandId,
                categoryId,
                isActive = true,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProduct_Should_Allow_Renaming_To_Its_Own_Existing_Slug()
    {
        // The slug-conflict check excludes the product's own row (p.Id != product.Id), so a
        // no-op rename to the same name must NOT trip the conflict branch.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);
        var name = UniqueName("SelfRename");
        var productId = await CreateProductAsync(client, brandId, categoryId, name: name);

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}",
            new
            {
                productId,
                name, // same name → same slug, but it's the same row
                description = "edited but kept the name",
                brandId,
                categoryId,
                isActive = true,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProduct_Should_Return404_When_Product_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await CreateBrandAndCategoryAsync(client);

        var unknownId = Guid.NewGuid();
        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{unknownId}",
            new
            {
                productId = unknownId,
                name = UniqueName("Ghost"),
                description = (string?)null,
                brandId,
                categoryId,
                isActive = true,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"UpdBranch-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<(Guid BrandId, Guid CategoryId)> CreateBrandAndCategoryAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var brandResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/brands",
            new { name = $"upd-brand-{suffix}", description = (string?)null, logoUrl = (string?)null });
        brandResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup helper failed to create brand: {await brandResp.Content.ReadAsStringAsync()}");
        var brandId = await brandResp.DeserializeAsync<Guid>();

        using var categoryResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories",
            new { name = $"upd-cat-{suffix}", description = (string?)null, parentCategoryId = (Guid?)null });
        categoryResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup helper failed to create category: {await categoryResp.Content.ReadAsStringAsync()}");
        var categoryId = await categoryResp.DeserializeAsync<Guid>();

        return (brandId, categoryId);
    }

    private static async Task<Guid> CreateProductAsync(
        HttpClient client, Guid brandId, Guid categoryId, string? name = null)
    {
        var sku = "UB-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        using var createResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku,
                name = name ?? $"Update product {sku}",
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
}
