#pragma warning disable S1144 // Unused private members — populated by JSON deserialization
#pragma warning disable S3459 // Unassigned members — populated by JSON deserialization
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Coverage for the tenant subscription upgrade endpoint
/// (<c>POST /api/v1/tenants/{id}/upgrade</c>): happy path, route/body mismatch,
/// validation (empty tenant, past expiry date), authorization (the permission is
/// root-only), and cross-tenant safety (a tenant operator cannot upgrade anyone).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class UpgradeTenantTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public UpgradeTenantTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task UpgradeTenant_Should_ExtendValidity_When_DateIsInFuture()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"upg-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"upg-{unique}@tenant.com");

        var newExpiry = DateTime.UtcNow.AddYears(2).Date.AddHours(12);

        // Act
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/upgrade",
            new { tenant = tenantId, extendedExpiryDate = newExpiry });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpgradeResult>(Json);
        result.ShouldNotBeNull();
        result.Tenant.ShouldBe(tenantId);
        result.NewValidity.ShouldBe(newExpiry, tolerance: TimeSpan.FromSeconds(1));

        // Status endpoint should reflect the new validity.
        var statusResponse = await rootClient.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/status");
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var status = await statusResponse.Content.ReadFromJsonAsync<TenantStatus>(Json);
        status.ShouldNotBeNull();
        status.ValidUpto!.Value.ShouldBe(newExpiry, tolerance: TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Validation / Bad Request

    [Fact]
    public async Task UpgradeTenant_Should_Return400_When_RouteIdDoesNotMatchBody()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"upg-mismatch-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"upg-mismatch-{unique}@tenant.com");

        // Act — body Tenant differs from the route id.
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/upgrade",
            new { tenant = "some-other-tenant", extendedExpiryDate = DateTime.UtcNow.AddYears(1) });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpgradeTenant_Should_Return400_When_ExpiryDateIsInThePast()
    {
        // Arrange — validator requires ExtendedExpiryDate > now.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"upg-past-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"upg-past-{unique}@tenant.com");

        // Act
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/upgrade",
            new { tenant = tenantId, extendedExpiryDate = DateTime.UtcNow.AddDays(-1) });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpgradeTenant_Should_Return400_When_TenantIsEmpty()
    {
        // Arrange — empty body tenant fails the NotEmpty rule. Route id is also empty
        // string equivalent via a single segment, so we use a real route id but blank body.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"upg-empty-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"upg-empty-{unique}@tenant.com");

        // Act — body tenant is empty; route id is non-empty so the endpoint mismatch
        // guard returns 400 before the validator, but either way the result is 400.
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/upgrade",
            new { tenant = "", extendedExpiryDate = DateTime.UtcNow.AddYears(1) });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region AuthZ

    [Fact]
    public async Task UpgradeTenant_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/anytenant/upgrade",
            new { tenant = "anytenant", extendedExpiryDate = DateTime.UtcNow.AddYears(1) });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpgradeTenant_Should_Forbid_When_CallerIsTenantAdmin()
    {
        // Arrange — provision a tenant and authenticate as its (non-root) admin.
        // The Tenants.Update permission is root-only, so the tenant admin lacks it.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"upg-authz-{unique}";
        var adminEmail = $"upg-authz-{unique}@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantClient = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        // Act — tenant admin attempts to upgrade its own tenant.
        var response = await tenantClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/upgrade",
            new { tenant = tenantId, extendedExpiryDate = DateTime.UtcNow.AddYears(1) });

        // Assert — denied (the root-only permission is missing).
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Helpers

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (var i = 0; i < maxRetries; i++)
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
            name = $"Upgrade {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Tenant {tenantId} provisioning failed: {content}");
                }
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Tenant {tenantId} did not finish provisioning.");
    }

    private sealed record UpgradeResult(DateTime NewValidity, string Tenant);

    private sealed record TenantStatus
    {
        public string Id { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime? ValidUpto { get; init; }
    }

    #endregion
}
