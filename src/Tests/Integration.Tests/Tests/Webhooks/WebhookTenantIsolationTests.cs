using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Webhooks;

[Collection(FshCollectionDefinition.Name)]
public sealed class WebhookTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public WebhookTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetSubscriptions_Should_OnlyReturnCurrentTenantsSubscriptions()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"webhook-iso-{uniqueId}";
        var otherAdminEmail = $"webhook-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        var rootMarker = $"root-only-{uniqueId}";
        var rootCreate = await rootClient.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions", new
            {
                url = $"https://example.com/{rootMarker}",
                events = new[] { "user.created" },
                secret = "root-secret"
            });
        rootCreate.StatusCode.ShouldBe(HttpStatusCode.Created);

        var otherCreate = await otherClient.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions", new
            {
                url = $"https://example.com/other-{uniqueId}",
                events = new[] { "user.created" },
                secret = "other-secret"
            });
        otherCreate.StatusCode.ShouldBe(HttpStatusCode.Created);

        var otherListResponse = await otherClient.GetAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions?pageNumber=1&pageSize=100");
        otherListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var otherListBody = await otherListResponse.Content.ReadAsStringAsync();

        otherListBody.ShouldNotContain(rootMarker);
        otherListBody.ShouldContain($"other-{uniqueId}");
    }

    [Fact]
    public async Task DeleteSubscription_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"webhook-del-{uniqueId}";
        var otherAdminEmail = $"webhook-deladmin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        var rootCreate = await rootClient.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions", new
            {
                url = $"https://example.com/del-target-{uniqueId}",
                events = new[] { "user.deleted" },
                secret = "del-secret"
            });
        rootCreate.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rootSubId = await rootCreate.Content.ReadFromJsonAsync<Guid>();

        var crossDelete = await otherClient.DeleteAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{rootSubId}");
        crossDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var ownDelete = await rootClient.DeleteAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{rootSubId}");
        ownDelete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TestSubscription_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"webhook-test-{uniqueId}";
        var otherAdminEmail = $"webhook-testadmin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        var rootCreate = await rootClient.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions", new
            {
                url = $"https://example.com/test-target-{uniqueId}",
                events = new[] { "webhook.test" },
                secret = "test-secret"
            });
        rootCreate.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rootSubId = await rootCreate.Content.ReadFromJsonAsync<Guid>();

        var crossTrigger = await otherClient.PostAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{rootSubId}/test",
            content: null);
        crossTrigger.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
