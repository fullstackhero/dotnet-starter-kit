using FSH.Modules.Catalog.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// Cross-TENANT isolation for the catalog module. Proves a product (and the
/// brand/category it depends on) created in tenant A (root) is completely
/// invisible to tenant B: B cannot fetch it, list it, or delete it — every
/// cross-tenant access returns 404, never a leak. The CatalogDbContext gets
/// tenant isolation via BaseDbContext's auto-apply, so these assert the
/// intended behavior. Intra-tenant CRUD lives in <see cref="ProductsEndpointTests"/>.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class CatalogTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public CatalogTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetProductById_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Arrange — tenant A (root) creates a product; tenant B is freshly provisioned.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"catalog-get-{uniqueId}");

        var productId = await CreateProductAsync(rootClient);

        // Act — tenant B tries to fetch tenant A's product.
        using var crossGet = await otherClient.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");

        // Assert — must be a clean 404, never tenant A's data.
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Sanity: tenant A still sees its own product.
        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchProducts_Should_NotReturn_OtherTenants_Products()
    {
        // Arrange.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"catalog-list-{uniqueId}");

        var rootName = $"Product-RootOnly-{uniqueId}";
        var productId = await CreateProductAsync(rootClient, name: rootName);

        // Act — tenant B lists products.
        using var listResponse = await otherClient.GetAsync(
            $"{TestConstants.CatalogBasePath}/products?pageNumber=1&pageSize=200");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await listResponse.DeserializeAsync<PagedResult<ProductDto>>();
        var body = await otherClient.GetStringAsync(
            $"{TestConstants.CatalogBasePath}/products?pageNumber=1&pageSize=200");

        // Assert — tenant A's product never appears in tenant B's listing.
        page.Items.ShouldNotContain(p => p.Id == productId,
            "tenant B's product list must not include tenant A's product");
        body.ShouldNotContain(rootName);
    }

    [Fact]
    public async Task DeleteProduct_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Arrange.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"catalog-del-{uniqueId}");

        var productId = await CreateProductAsync(rootClient);

        // Act — tenant B tries to delete tenant A's product.
        using var crossDelete = await otherClient.DeleteAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");

        // Assert — 404 (not 204): the mutation never reaches tenant A's row.
        crossDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Sanity: tenant A's product is untouched and still fetchable.
        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.CatalogBasePath}/products/{productId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"Catalog-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static string UniqueSku(string prefix) =>
        $"TST-{prefix.ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    /// <summary>
    /// Creates a brand + category + product owned by whichever tenant <paramref name="client"/>
    /// is authenticated for, returning the new product id. Catalog data is no longer
    /// auto-seeded per tenant, so each test creates its own foreign keys.
    /// </summary>
    private static async Task<Guid> CreateProductAsync(HttpClient client, string? name = null)
    {
        using var brandResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/brands",
            new { name = UniqueName("Brand"), description = (string?)null, logoUrl = (string?)null });
        brandResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create brand: {await brandResp.Content.ReadAsStringAsync()}");
        var brandId = await brandResp.DeserializeAsync<Guid>();

        using var categoryResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories",
            new { name = UniqueName("Cat"), description = (string?)null, parentCategoryId = (Guid?)null });
        categoryResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create category: {await categoryResp.Content.ReadAsStringAsync()}");
        var categoryId = await categoryResp.DeserializeAsync<Guid>();

        var sku = UniqueSku("Iso");
        using var productResp = await client.PostAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/products",
            new
            {
                sku,
                name = name ?? $"Test product {sku}",
                description = (string?)null,
                brandId,
                categoryId,
                priceAmount = 1m,
                priceCurrency = "USD",
                stock = 1,
            });
        productResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create product: {await productResp.Content.ReadAsStringAsync()}");
        return await productResp.DeserializeAsync<Guid>();
    }

    private async Task<HttpClient> ProvisionTenantClientAsync(HttpClient rootClient, string tenantId)
    {
        var adminEmail = $"{tenantId}-admin@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);
        return await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
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

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed: {content}");
                }
            }

            await Task.Delay(1000);
        }

        var finalResponse = await client.GetAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
        var finalContent = finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadAsStringAsync()
            : $"HTTP {finalResponse.StatusCode}";

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. Last status: {finalContent}");
    }
}
