using FSH.Modules.Catalog.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// End-to-end coverage for the Category CRUD endpoints. Mirrors the BrandsEndpointTests
/// surface (happy path, soft-delete + restore, business rules, auth gating) so the third
/// leg of the catalog API has parity with Brands and Products. Category-specific behaviour
/// — the parent/child hierarchy, cycle detection, and tree retrieval — gets its own group.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class CategoriesEndpointTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public CategoriesEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_Should_Return200_And_Persist_When_AuthorizedAdmin()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("CreateOk");

        using var createResponse = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name,
            description = "Category created by integration test",
            parentCategoryId = (Guid?)null,
        });

        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var categoryId = await createResponse.DeserializeAsync<Guid>();
        categoryId.ShouldNotBe(Guid.Empty);

        using var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories/{categoryId}");
        var fetched = await getResponse.DeserializeAsync<CategoryDto>();

        fetched.Name.ShouldBe(name);
        fetched.Description.ShouldBe("Category created by integration test");
        fetched.ParentCategoryId.ShouldBeNull();
        fetched.Slug.ShouldNotBeNullOrWhiteSpace();
        fetched.Slug.ShouldNotContain(" ");
    }

    [Fact]
    public async Task SearchCategories_Should_Include_NewlyCreated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Searchable");

        await CreateAsync(client, name);

        using var listResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/categories?search={Uri.EscapeDataString(name)}&pageNumber=1&pageSize=20");

        var page = await listResponse.DeserializeAsync<PagedResult<CategoryDto>>();
        page.Items.ShouldContain(c => c.Name == name);
    }

    [Fact]
    public async Task UpdateCategory_Should_PersistChanges()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var categoryId = await CreateAsync(client, UniqueName("Updatable"));
        var newName = UniqueName("Updated");

        using var updateResponse = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories/{categoryId}",
            new { categoryId, name = newName, description = "Updated description", parentCategoryId = (Guid?)null });

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories/{categoryId}");
        var updated = await getResponse.DeserializeAsync<CategoryDto>();

        updated.Name.ShouldBe(newName);
        updated.Description.ShouldBe("Updated description");
        updated.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteCategory_Should_RemoveCategory()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var categoryId = await CreateAsync(client, UniqueName("Deletable"));

        using var deleteResponse = await client.DeleteAsync($"{TestConstants.CatalogBasePath}/categories/{categoryId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories/{categoryId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── hierarchy ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_Should_Persist_ParentRelationship_When_ParentExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var parentId = await CreateAsync(client, UniqueName("Parent"));

        var childName = UniqueName("Child");
        using var createResponse = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name = childName,
            description = (string?)null,
            parentCategoryId = parentId,
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var childId = await createResponse.DeserializeAsync<Guid>();

        using var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories/{childId}");
        var child = await getResponse.DeserializeAsync<CategoryDto>();
        child.ParentCategoryId.ShouldBe(parentId);
    }

    [Fact]
    public async Task SearchCategories_Should_Filter_By_ParentCategoryId()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var parentId = await CreateAsync(client, UniqueName("FilterParent"));
        var childName = UniqueName("FilterChild");
        var childId = await CreateAsync(client, childName, parentId);

        using var listResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/categories?parentCategoryId={parentId}&pageNumber=1&pageSize=50");

        var page = await listResponse.DeserializeAsync<PagedResult<CategoryDto>>();
        page.Items.ShouldContain(c => c.Id == childId);
        page.Items.ShouldAllBe(c => c.ParentCategoryId == parentId);
    }

    [Fact]
    public async Task GetCategoryTree_Should_Expose_Child_Under_Parent()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var parentName = UniqueName("TreeParent");
        var parentId = await CreateAsync(client, parentName);
        var childName = UniqueName("TreeChild");
        var childId = await CreateAsync(client, childName, parentId);

        using var treeResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories/tree");
        treeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tree = await treeResponse.DeserializeAsync<IReadOnlyList<CategoryTreeNodeDto>>();

        var parentNode = tree.FirstOrDefault(n => n.Id == parentId);
        parentNode.ShouldNotBeNull("the freshly created parent must appear at the root level of the tree");
        parentNode!.Children.ShouldContain(n => n.Id == childId,
            "the child must be nested under its parent in the tree response");
    }

    [Fact]
    public async Task UpdateCategory_Should_Return400_When_Set_As_Own_Parent()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var categoryId = await CreateAsync(client, UniqueName("SelfParent"));

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories/{categoryId}",
            new { categoryId, name = UniqueName("Renamed"), description = (string?)null, parentCategoryId = categoryId });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_Should_Return400_When_Parent_Change_Would_Create_Cycle()
    {
        // grand → parent → child (current shape). Re-parenting `grand` under `child` would close
        // the loop. The handler walks the parent chain and must reject that.
        using var client = await _auth.CreateRootAdminClientAsync();
        var grandId = await CreateAsync(client, UniqueName("Grand"));
        var parentId = await CreateAsync(client, UniqueName("Parent"), grandId);
        var childId = await CreateAsync(client, UniqueName("Child"), parentId);

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories/{grandId}",
            new { categoryId = grandId, name = UniqueName("GrandRenamed"), description = (string?)null, parentCategoryId = childId });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteCategory_Should_Return409_When_Has_Child_Categories()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var parentId = await CreateAsync(client, UniqueName("HasChildren"));
        _ = await CreateAsync(client, UniqueName("OrphanWaiting"), parentId);

        using var deleteResponse = await client.DeleteAsync($"{TestConstants.CatalogBasePath}/categories/{parentId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ─── soft delete + restore ───────────────────────────────────────

    [Fact]
    public async Task DeleteCategory_Should_HideFromSearch_But_Keep_Row_For_Restore()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Soft");
        var categoryId = await CreateAsync(client, name);

        using var deleteResponse = await client.DeleteAsync($"{TestConstants.CatalogBasePath}/categories/{categoryId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Search excludes soft-deleted categories — global query filter.
        using var searchResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/categories?search={Uri.EscapeDataString(name)}&pageNumber=1&pageSize=20");
        var page = await searchResponse.DeserializeAsync<PagedResult<CategoryDto>>();
        page.Items.ShouldNotContain(c => c.Id == categoryId,
            "Search must not return soft-deleted categories.");

        // Trash listing bypasses the filter and surfaces audit stamps.
        using var trashResponse = await client.GetAsync(
            $"{TestConstants.CatalogBasePath}/categories/trash?pageNumber=1&pageSize=50");
        trashResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trash = await trashResponse.DeserializeAsync<PagedResult<CategoryDto>>();
        var trashed = trash.Items.FirstOrDefault(c => c.Id == categoryId);
        trashed.ShouldNotBeNull("Soft-deleted category should appear in /categories/trash.");
        trashed!.DeletedOnUtc.ShouldNotBeNull();
        trashed.DeletedBy.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RestoreCategory_Should_BringBack_DeletedCategory()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Restorable");
        var categoryId = await CreateAsync(client, name);

        await client.DeleteAsync($"{TestConstants.CatalogBasePath}/categories/{categoryId}");

        using var restoreResponse = await client.PostAsync(
            $"{TestConstants.CatalogBasePath}/categories/{categoryId}/restore", content: null);
        restoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // After restore, GetById succeeds and the category is visible again.
        using var getResponse = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories/{categoryId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await getResponse.DeserializeAsync<CategoryDto>();
        fetched.Name.ShouldBe(name);
        fetched.DeletedOnUtc.ShouldBeNull();
        fetched.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task CreateCategory_Should_Succeed_When_NameMatchesSoftDeletedCategory()
    {
        // Filtered unique index on Slug excludes soft-deleted rows, so a name can be reused after
        // a delete — a 409 should only fire when a *live* category holds the conflicting slug.
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Reusable");
        var firstId = await CreateAsync(client, name);

        await client.DeleteAsync($"{TestConstants.CatalogBasePath}/categories/{firstId}");

        using var secondCreate = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name,
            description = (string?)null,
            parentCategoryId = (Guid?)null,
        });
        secondCreate.StatusCode.ShouldBe(HttpStatusCode.OK);

        var secondId = await secondCreate.DeserializeAsync<Guid>();
        secondId.ShouldNotBe(firstId);
    }

    [Fact]
    public async Task RestoreCategory_Should_Return404_When_CategoryDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsync(
            $"{TestConstants.CatalogBasePath}/categories/{Guid.NewGuid()}/restore", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── business rules ──────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_Should_Return409_When_NameAlreadyExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Duplicate");

        await CreateAsync(client, name);

        using var conflictResponse = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name,
            description = (string?)null,
            parentCategoryId = (Guid?)null,
        });

        conflictResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCategory_Should_Return400_When_NameIsEmpty()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name = "",
            description = (string?)null,
            parentCategoryId = (Guid?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_Should_Return404_When_Parent_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name = UniqueName("BadParent"),
            description = (string?)null,
            parentCategoryId = Guid.NewGuid(),
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCategoryById_Should_Return404_When_CategoryDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_Should_Return404_When_CategoryDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var unknownId = Guid.NewGuid();

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.CatalogBasePath}/categories/{unknownId}",
            new { categoryId = unknownId, name = UniqueName("Ghost"), description = (string?)null, parentCategoryId = (Guid?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── auth gating ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name = UniqueName("Unauthed"),
            description = (string?)null,
            parentCategoryId = (Guid?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchCategories_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories?pageNumber=1&pageSize=20");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategoryTree_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync($"{TestConstants.CatalogBasePath}/categories/tree");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── idempotency ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_Should_NotMarkReplayed_When_NoIdempotencyKey()
    {
        // Same check the Brands/Products tests carry — Categories' POST opts in to the
        // Idempotency filter, so a request without a key must never come back marked as a replay.
        const string ReplayedHeader = "Idempotency-Replayed";

        using var client = await _auth.CreateRootAdminClientAsync();
        using var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name = UniqueName("NoIdem"),
            description = (string?)null,
            parentCategoryId = (Guid?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains(ReplayedHeader).ShouldBeFalse(
            "A request without an idempotency key must never be marked as replayed.");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"Category-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<Guid> CreateAsync(HttpClient client, string name, Guid? parentCategoryId = null)
    {
        using var response = await client.PostAsJsonAsync($"{TestConstants.CatalogBasePath}/categories", new
        {
            name,
            description = (string?)null,
            parentCategoryId,
        });
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"CreateCategory failed: {response.StatusCode}\n{body}");
        }
        return await response.DeserializeAsync<Guid>();
    }
}
