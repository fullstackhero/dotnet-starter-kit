#pragma warning disable S1144 // Unused private members — populated by JSON deserialization
#pragma warning disable S3459 // Unassigned members — populated by JSON deserialization
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Coverage for the per-tenant migration-status query
/// (<c>GET /api/v1/tenants/migrations</c>). The handler enumerates every tenant in
/// the store, opens a scoped DbContext under each tenant's context, and reports the
/// applied/pending migrations. These tests assert the root tenant and any freshly
/// provisioned tenant appear in the report with provider info and no pending
/// migrations, plus the authentication contract.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TenantMigrationsTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private const string MigrationsPath = $"{TestConstants.TenantsBasePath}/migrations";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TenantMigrationsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task GetMigrations_Should_IncludeRootTenant_WithProviderAndNoPending()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await rootClient.GetAsync(MigrationsPath);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statuses = await response.Content.ReadFromJsonAsync<List<MigrationStatus>>(Json);
        statuses.ShouldNotBeNull();

        var root = statuses.SingleOrDefault(s => s.TenantId == TestConstants.RootTenantId);
        root.ShouldNotBeNull("root tenant must appear in the migration report");
        root.Error.ShouldBeNull();
        root.Provider.ShouldNotBeNullOrEmpty();
        root.Provider.ShouldContain("Npgsql");
        root.HasPendingMigrations.ShouldBeFalse();
        root.LastAppliedMigration.ShouldNotBeNullOrEmpty();
        root.PendingMigrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMigrations_Should_IncludeNewlyProvisionedTenant_When_ProvisioningCompleted()
    {
        // Arrange — provision a fresh tenant.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"mig-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"mig-{unique}@tenant.com");
        await WaitForProvisioningAsync(rootClient, tenantId);

        // Act
        var response = await rootClient.GetAsync(MigrationsPath);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statuses = await response.Content.ReadFromJsonAsync<List<MigrationStatus>>(Json);
        statuses.ShouldNotBeNull();

        var tenant = statuses.SingleOrDefault(s => s.TenantId == tenantId);
        tenant.ShouldNotBeNull($"provisioned tenant {tenantId} must appear in the report");
        tenant.IsActive.ShouldBeTrue();
        tenant.Error.ShouldBeNull();
        tenant.HasPendingMigrations.ShouldBeFalse();
        tenant.LastAppliedMigration.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region AuthZ

    [Fact]
    public async Task GetMigrations_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.GetAsync(MigrationsPath);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMigrations_Should_Forbid_When_CallerIsTenantAdmin()
    {
        // Arrange — Tenants.View is a root-only permission; a tenant admin lacks it.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"mig-authz-{unique}";
        var adminEmail = $"mig-authz-{unique}@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantClient = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        // Act
        var response = await tenantClient.GetAsync(MigrationsPath);

        // Assert
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
            name = $"Migrations {tenantId}",
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

    private sealed record MigrationStatus
    {
        public string TenantId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime? ValidUpto { get; init; }
        public bool HasPendingMigrations { get; init; }
        public string? Provider { get; init; }
        public string? LastAppliedMigration { get; init; }
        public IReadOnlyCollection<string> PendingMigrations { get; init; } = Array.Empty<string>();
        public string? Error { get; init; }
    }

    #endregion
}
