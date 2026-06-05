using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Catalog;

[Collection(FshCollectionDefinition.Name)]
public sealed class BrandsEndpointTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public BrandsEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateBrand_Should_Return200_And_Persist_When_AuthorizedAdmin()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("CreateOk");

        var createResponse = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/brands", new
        {
            name,
            description = "Brand created by integration test",
            logoUrl = (string?)null,
        });

        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var brandId = await createResponse.DeserializeAsync<Guid>();
        brandId.ShouldNotBe(Guid.Empty);

        var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/brands/{brandId}");
        var fetched = await getResponse.DeserializeAsync<BrandDto>();

        fetched.Name.ShouldBe(name);
        fetched.Description.ShouldBe("Brand created by integration test");
        fetched.Slug.ShouldNotBeNullOrWhiteSpace();
        fetched.Slug.ShouldNotContain(" ");
    }

    [Fact]
    public async Task SearchBrands_Should_Include_NewlyCreated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Searchable");

        await CreateAsync(client, name);

        var listResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/brands?search={Uri.EscapeDataString(name)}&pageNumber=1&pageSize=20");

        var page = await listResponse.DeserializeAsync<PagedResult<BrandDto>>();
        page.Items.ShouldContain(b => b.Name == name);
    }

    [Fact]
    public async Task UpdateBrand_Should_PersistChanges()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var brandId = await CreateAsync(client, UniqueName("Updatable"));
        var newName = UniqueName("Updated");

        var updateResponse = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/brands/{brandId}",
            new { brandId, name = newName, description = "Updated description", logoUrl = (string?)null });

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/brands/{brandId}");
        var updated = await getResponse.DeserializeAsync<BrandDto>();

        updated.Name.ShouldBe(newName);
        updated.Description.ShouldBe("Updated description");
        updated.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteBrand_Should_RemoveBrand()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var brandId = await CreateAsync(client, UniqueName("Deletable"));

        var deleteResponse = await client.DeleteAsync($"{TestConstants.CatalogBasePath}/brands/{brandId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/brands/{brandId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── soft delete + restore ───────────────────────────────────────

    [Fact]
    public async Task DeleteBrand_Should_HideFromSearch_But_Keep_Row_For_Restore()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Soft");
        var brandId = await CreateAsync(client, name);

        var deleteResponse = await client.DeleteAsync($"{TestConstants.CatalogBasePath}/brands/{brandId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Search must not include the deleted brand — the global query filter excludes it.
        var searchResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/brands?search={Uri.EscapeDataString(name)}&pageNumber=1&pageSize=20");
        var page = await searchResponse.DeserializeAsync<PagedResult<BrandDto>>();
        page.Items.ShouldNotContain(b => b.Id == brandId,
            "Search must not return soft-deleted brands.");

        // Trash listing should — it bypasses the filter.
        var trashResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/brands/trash?pageNumber=1&pageSize=50");
        trashResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trash = await trashResponse.DeserializeAsync<PagedResult<BrandDto>>();
        var trashed = trash.Items.FirstOrDefault(b => b.Id == brandId);
        trashed.ShouldNotBeNull("Soft-deleted brand should appear in /brands/trash.");
        trashed!.DeletedOnUtc.ShouldNotBeNull();
        trashed.DeletedBy.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RestoreBrand_Should_BringBack_DeletedBrand()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Restorable");
        var brandId = await CreateAsync(client, name);

        await client.DeleteAsync($"{TestConstants.CatalogBasePath}/brands/{brandId}");

        var restoreResponse = await client.PostAsync(
            $"{TestConstants.CatalogBasePath}/brands/{brandId}/restore", content: null);
        restoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // After restore, GetById succeeds and the brand is visible again.
        var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/brands/{brandId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await getResponse.DeserializeAsync<BrandDto>();
        fetched.Name.ShouldBe(name);
        fetched.DeletedOnUtc.ShouldBeNull();
        fetched.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task CreateBrand_Should_Succeed_When_NameMatchesSoftDeletedBrand()
    {
        // Filtered unique index on Slug excludes soft-deleted rows, so a name can be reused after
        // a delete — a 409 should only fire when a *live* brand holds the conflicting slug.
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Reusable");
        var firstId = await CreateAsync(client, name);

        await client.DeleteAsync($"{TestConstants.CatalogBasePath}/brands/{firstId}");

        var secondCreate = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/brands", new
        {
            name,
            description = (string?)null,
            logoUrl = (string?)null,
        });
        secondCreate.StatusCode.ShouldBe(HttpStatusCode.OK);

        var secondId = await secondCreate.DeserializeAsync<Guid>();
        secondId.ShouldNotBe(firstId);
    }

    [Fact]
    public async Task RestoreBrand_Should_Return404_When_BrandDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.PostAsync(
            $"{TestConstants.CatalogBasePath}/brands/{Guid.NewGuid()}/restore", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── business rules ──────────────────────────────────────────────

    [Fact]
    public async Task CreateBrand_Should_Return409_When_NameAlreadyExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Duplicate");

        await CreateAsync(client, name);

        var conflictResponse = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/brands", new
        {
            name,
            description = (string?)null,
            logoUrl = (string?)null,
        });

        conflictResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBrand_Should_Return400_When_NameIsEmpty()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/brands", new
        {
            name = "",
            description = (string?)null,
            logoUrl = (string?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBrandById_Should_Return404_When_BrandDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.CatalogBasePath}/brands/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── auth gating ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateBrand_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/brands", new
        {
            name = UniqueName("Unauthed"),
            description = (string?)null,
            logoUrl = (string?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchBrands_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync($"{TestConstants.CatalogBasePath}/brands?pageNumber=1&pageSize=20");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── idempotency ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateBrand_Should_NotMarkReplayed_When_NoIdempotencyKey()
    {
        // Same-key replay is verified globally in IdempotencyFilterTests; here we just confirm
        // Brands' POST without a key behaves normally, catching accidental opt-in regressions.
        const string ReplayedHeader = "Idempotency-Replayed";

        using var client = await _auth.CreateRootAdminClientAsync();
        var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/brands", new
        {
            name = UniqueName("NoIdem"),
            description = (string?)null,
            logoUrl = (string?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains(ReplayedHeader).ShouldBeFalse(
            "A request without an idempotency key must never be marked as replayed.");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"Brand-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<Guid> CreateAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/brands", new
        {
            name,
            description = (string?)null,
            logoUrl = (string?)null,
        });
        return await response.DeserializeAsync<Guid>();
    }

}
