using FSH.Modules.Catalog.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// End-to-end coverage for the product CRUD endpoints. Mirrors the BrandsEndpointTests
/// surface (happy-path, soft-delete + restore, business rules, auth gating) so the two
/// halves of the catalog API have parity. Image-specific endpoints stay in
/// <see cref="ProductImagesTests"/>; this file is only the product lifecycle.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ProductsEndpointTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ProductsEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_Should_Return200_And_Persist_When_AuthorizedAdmin()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var sku = UniqueSku("Create");

        using var createResponse = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku,
                name = $"Test product {sku}",
                description = "Created by integration test",
                brandId,
                categoryId,
                priceAmount = 19.99m,
                priceCurrency = "USD",
                stock = 10,
            });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var productId = await createResponse.DeserializeAsync<Guid>();
        productId.ShouldNotBe(Guid.Empty);

        using var getResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");
        var fetched = await getResponse.DeserializeAsync<ProductDto>();

        fetched.Sku.ShouldBe(sku);
        fetched.Name.ShouldBe($"Test product {sku}");
        fetched.Description.ShouldBe("Created by integration test");
        fetched.BrandId.ShouldBe(brandId);
        fetched.CategoryId.ShouldBe(categoryId);
        fetched.Price.Amount.ShouldBe(19.99m);
        fetched.Price.Currency.ShouldBe("USD");
        fetched.Stock.ShouldBe(10);
        fetched.IsActive.ShouldBeTrue();
        fetched.Slug.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SearchProducts_Should_Include_NewlyCreated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var productId = await CreateAsync(client, brandId, categoryId, name: UniqueName("Searchable"));

        using var listResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products?pageNumber=1&pageSize=200");
        var page = await listResponse.DeserializeAsync<PagedResult<ProductDto>>();

        page.Items.ShouldContain(p => p.Id == productId);
    }

    [Fact]
    public async Task SearchProducts_Should_Filter_By_BrandId()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var productId = await CreateAsync(client, brandId, categoryId, name: UniqueName("Filter-Brand"));

        using var listResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products?brandId={brandId}&pageNumber=1&pageSize=50");
        var page = await listResponse.DeserializeAsync<PagedResult<ProductDto>>();

        page.Items.ShouldContain(p => p.Id == productId);
        page.Items.ShouldAllBe(p => p.BrandId == brandId);
    }

    [Fact]
    public async Task UpdateProduct_Should_PersistChanges()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var productId = await CreateAsync(client, brandId, categoryId);
        var newName = UniqueName("Updated");

        using var updateResponse = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}",
            new
            {
                productId,
                name = newName,
                description = "Updated description",
                brandId,
                categoryId,
                isActive = false,
            });
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var getResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");
        var updated = await getResponse.DeserializeAsync<ProductDto>();

        updated.Name.ShouldBe(newName);
        updated.Description.ShouldBe("Updated description");
        updated.IsActive.ShouldBeFalse();
        updated.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task ChangeProductPrice_Should_Persist_New_Price()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var productId = await CreateAsync(client, brandId, categoryId);

        using var priceResponse = await client.PatchAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/price",
            new { productId, amount = 42.50m, currency = "USD" });
        priceResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = await GetAsync(client, productId);
        updated.Price.Amount.ShouldBe(42.50m);
        updated.Price.Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task AdjustProductStock_Should_Apply_Positive_Delta()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var productId = await CreateAsync(client, brandId, categoryId, stock: 5);

        using var adjust = await client.PatchAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/stock",
            new { productId, delta = 7 });
        adjust.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await GetAsync(client, productId);
        fetched.Stock.ShouldBe(12);
    }

    [Fact]
    public async Task AdjustProductStock_Should_Apply_Negative_Delta_When_In_Range()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var productId = await CreateAsync(client, brandId, categoryId, stock: 10);

        using var adjust = await client.PatchAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/stock",
            new { productId, delta = -4 });
        adjust.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await GetAsync(client, productId);
        fetched.Stock.ShouldBe(6);
    }

    [Fact]
    public async Task AdjustProductStock_Should_Reject_Negative_Delta_That_Goes_Below_Zero()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var productId = await CreateAsync(client, brandId, categoryId, stock: 3);

        using var adjust = await client.PatchAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/stock",
            new { productId, delta = -10 });

        // Domain guards against negative stock — handler converts to a 4xx via CustomException.
        adjust.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);

        var fetched = await GetAsync(client, productId);
        fetched.Stock.ShouldBe(3, "stock must be unchanged after a rejected adjustment");
    }

    // ─── soft delete + restore ───────────────────────────────────────

    [Fact]
    public async Task DeleteProduct_Should_Hide_From_Search_But_Keep_Row_For_Restore()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var name = UniqueName("Soft");
        var productId = await CreateAsync(client, brandId, categoryId, name: name);

        using var deleteResponse = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Search excludes soft-deleted products (global query filter).
        using var searchResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products?search={Uri.EscapeDataString(name)}&pageNumber=1&pageSize=50");
        var searchPage = await searchResponse.DeserializeAsync<PagedResult<ProductDto>>();
        searchPage.Items.ShouldNotContain(p => p.Id == productId,
            "search must not return soft-deleted products");

        // GetById on a deleted product returns 404.
        using var getResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Trash listing surfaces the soft-deleted row, with audit stamps.
        using var trashResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/trash?pageNumber=1&pageSize=50");
        trashResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trash = await trashResponse.DeserializeAsync<PagedResult<ProductDto>>();
        var trashed = trash.Items.FirstOrDefault(p => p.Id == productId);
        trashed.ShouldNotBeNull("soft-deleted product should appear in /products/trash");
        trashed!.DeletedOnUtc.ShouldNotBeNull();
        trashed.DeletedBy.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RestoreProduct_Should_BringBack_DeletedProduct()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var name = UniqueName("Restorable");
        var productId = await CreateAsync(client, brandId, categoryId, name: name);
        await client.DeleteAsync($"{TestConstants.CatalogBasePath}/products/{productId}");

        using var restoreResponse = await client.PostAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}/restore", content: null);
        restoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // After restore, GetById succeeds and the product is visible.
        var restored = await GetAsync(client, productId);
        restored.Name.ShouldBe(name);
        restored.DeletedOnUtc.ShouldBeNull();
        restored.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task RestoreProduct_Should_Return404_When_Product_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}/restore", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_Followed_By_CreateWithSameSku_Should_Succeed()
    {
        // Soft-delete keeps the row but the filtered unique index on SKU
        // excludes deleted rows — admins should be able to reuse the SKU.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var sku = UniqueSku("Reusable");
        var firstId = await CreateAsync(client, brandId, categoryId, sku: sku);

        await client.DeleteAsync($"{TestConstants.CatalogBasePath}/products/{firstId}");

        using var secondCreate = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku,
                name = $"Reusable {sku} v2",
                description = (string?)null,
                brandId,
                categoryId,
                priceAmount = 9.99m,
                priceCurrency = "USD",
                stock = 1,
            });
        secondCreate.StatusCode.ShouldBe(HttpStatusCode.OK);

        var secondId = await secondCreate.DeserializeAsync<Guid>();
        secondId.ShouldNotBe(firstId);
    }

    // ─── business rules ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_Should_Return409_When_Sku_Already_Exists_Live()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);
        var sku = UniqueSku("Dup");
        await CreateAsync(client, brandId, categoryId, sku: sku);

        using var conflict = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku,
                name = $"Conflict {sku}",
                description = (string?)null,
                brandId,
                categoryId,
                priceAmount = 1m,
                priceCurrency = "USD",
                stock = 0,
            });
        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateProduct_Should_Return400_When_Sku_Is_Empty()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku = "",
                name = "Has a name but empty SKU",
                description = (string?)null,
                brandId,
                categoryId,
                priceAmount = 1m,
                priceCurrency = "USD",
                stock = 0,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_Should_Return400_When_Price_Is_Negative()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku = UniqueSku("NegPrice"),
                name = "Negative price",
                description = (string?)null,
                brandId,
                categoryId,
                priceAmount = -1m,
                priceCurrency = "USD",
                stock = 1,
            });

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateProduct_Should_Return400_When_Brand_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (_, categoryId) = await PickBrandAndCategoryAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku = UniqueSku("BadBrand"),
                name = "Unknown brand",
                description = (string?)null,
                brandId = Guid.NewGuid(),
                categoryId,
                priceAmount = 1m,
                priceCurrency = "USD",
                stock = 1,
            });

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductById_Should_Return404_When_Product_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_Should_Return404_For_Unknown_Product()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── auth gating ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku = UniqueSku("Unauthed"),
                name = "Should fail without auth",
                description = (string?)null,
                brandId = Guid.NewGuid(),
                categoryId = Guid.NewGuid(),
                priceAmount = 1m,
                priceCurrency = "USD",
                stock = 1,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchProducts_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products?pageNumber=1&pageSize=20");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTrashedProducts_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/trash?pageNumber=1&pageSize=20");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── idempotency ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_Should_NotMarkReplayed_When_NoIdempotencyKey()
    {
        // Mirror of the BrandsEndpointTests check. Products' POST opts in to the
        // Idempotency filter — without a key, the response must never carry the
        // Idempotency-Replayed: true header.
        const string ReplayedHeader = "Idempotency-Replayed";

        using var client = await _auth.CreateRootAdminClientAsync();
        var (brandId, categoryId) = await PickBrandAndCategoryAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku = UniqueSku("NoIdem"),
                name = "NoIdem product",
                description = (string?)null,
                brandId,
                categoryId,
                priceAmount = 1m,
                priceCurrency = "USD",
                stock = 0,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains(ReplayedHeader).ShouldBeFalse(
            "A request without an idempotency key must never be marked as replayed.");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"Product-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    // The Product aggregate canonicalises the SKU to upper-case on Create, so
    // the helper emits an already-canonical form to keep assertions trivial.
    private static string UniqueSku(string prefix) =>
        $"TST-{prefix.ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static async Task<(Guid BrandId, Guid CategoryId)> PickBrandAndCategoryAsync(HttpClient client)
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

    private static async Task<Guid> CreateAsync(
        HttpClient client,
        Guid brandId,
        Guid categoryId,
        string? name = null,
        string? sku = null,
        int stock = 1)
    {
        var resolvedSku = sku ?? UniqueSku("Auto");
        var resolvedName = name ?? $"Test product {resolvedSku}";

        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku = resolvedSku,
                name = resolvedName,
                description = (string?)null,
                brandId,
                categoryId,
                priceAmount = 1m,
                priceCurrency = "USD",
                stock,
            });
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"CreateProduct failed: {response.StatusCode}\n{body}");
        }
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<ProductDto> GetAsync(HttpClient client, Guid productId)
    {
        using var response = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");
        return await response.DeserializeAsync<ProductDto>();
    }

    // Lightweight projection — we only need the category id from the list response.
#pragma warning disable S1144
    private sealed class CategoryRow
    {
        public Guid Id { get; set; }
    }
#pragma warning restore S1144
}
