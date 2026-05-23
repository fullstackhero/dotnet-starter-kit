using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Groups;

/// <summary>
/// Cross-TENANT isolation for identity groups. Proves a group created in tenant A
/// (root) is invisible to tenant B: B cannot fetch it, list it, update it, or
/// delete it — every cross-tenant access returns 404, never a leak. The
/// IdentityDbContext applies tenant isolation via ApplyTenantIsolationByDefault(),
/// so these assert the intended behavior. Intra-tenant CRUD lives in
/// <see cref="GroupCrudTests"/>.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class GroupTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public GroupTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetGroupById_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Arrange — tenant A (root) creates a group; tenant B is freshly provisioned.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"group-get-{uniqueId}");

        var groupId = await CreateGroupAsync(rootClient, $"RootOnly-{uniqueId}");

        // Act — tenant B tries to fetch tenant A's group.
        using var crossGet = await otherClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/groups/{groupId}");

        // Assert — clean 404, never tenant A's data.
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Sanity: tenant A still sees its own group.
        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/groups/{groupId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListGroups_Should_NotReturn_OtherTenants_Groups()
    {
        // Arrange.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"group-list-{uniqueId}");

        var rootName = $"RootOnly-{uniqueId}";
        var groupId = await CreateGroupAsync(rootClient, rootName);

        // Act — tenant B lists groups (endpoint returns a plain array).
        using var listResponse = await otherClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/groups");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var groups = await listResponse.DeserializeAsync<List<GroupDto>>();
        var body = await otherClient.GetStringAsync($"{TestConstants.IdentityBasePath}/groups");

        // Assert — tenant A's group never appears in tenant B's listing.
        groups.ShouldNotContain(g => g.Id == groupId,
            "tenant B's group list must not include tenant A's group");
        body.ShouldNotContain(rootName);
    }

    [Fact]
    public async Task UpdateGroup_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Arrange.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"group-upd-{uniqueId}");

        var rootName = $"RootOnly-{uniqueId}";
        var groupId = await CreateGroupAsync(rootClient, rootName);

        // Act — tenant B tries to mutate (update) tenant A's group.
        using var crossUpdate = await otherClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/groups/{groupId}",
            new
            {
                name = $"Hijacked-{uniqueId}",
                description = "should never apply",
                isDefault = false,
                roleIds = new List<string>()
            });

        // Assert — 404: the mutation never reaches tenant A's row.
        crossUpdate.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Sanity: tenant A's group is untouched — name unchanged.
        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/groups/{groupId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await ownGet.DeserializeAsync<GroupDto>();
        fetched.Name.ShouldBe(rootName);
    }

    [Fact]
    public async Task DeleteGroup_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Arrange.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"group-del-{uniqueId}");

        var groupId = await CreateGroupAsync(rootClient, $"RootOnly-{uniqueId}");

        // Act — tenant B tries to delete tenant A's group.
        using var crossDelete = await otherClient.DeleteAsync(
            $"{TestConstants.IdentityBasePath}/groups/{groupId}");

        // Assert — 404 (not 204): the delete never reaches tenant A's row.
        crossDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Sanity: tenant A's group still exists.
        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/groups/{groupId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<Guid> CreateGroupAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/groups",
            new
            {
                name,
                description = "Created by tenant-isolation test",
                isDefault = false,
                roleIds = new List<string>()
            });
        response.StatusCode.ShouldBe(HttpStatusCode.Created,
            $"setup failed to create group: {await response.Content.ReadAsStringAsync()}");
        var group = await response.DeserializeAsync<GroupDto>();
        return group.Id;
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
