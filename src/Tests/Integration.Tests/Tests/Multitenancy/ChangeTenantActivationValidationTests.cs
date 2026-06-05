#pragma warning disable S1144 // Unused private members — populated by JSON deserialization
#pragma warning disable S3459 // Unassigned members — populated by JSON deserialization
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Validation and edge-case coverage for the change-activation endpoint
/// (<c>POST /api/v1/tenants/{id}/activation</c>). Complements
/// <see cref="TenantActivationTests"/> (which covers the happy deactivate path) by
/// pinning the route/body mismatch guard, the not-found path, the root-tenant guard,
/// double-deactivation, and the deactivate -> reactivate round-trip on a provisioned
/// tenant — the branches in the validator/handler/TenantService that produce error
/// or terminal results.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ChangeTenantActivationValidationTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ChangeTenantActivationValidationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Bad Request / Validation

    [Fact]
    public async Task ChangeActivation_Should_Return400_When_RouteIdDoesNotMatchBody()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"act-mismatch-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"act-mismatch-{unique}@tenant.com");

        // Act — body TenantId differs from the route id.
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId = "different-id", isActive = false });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeActivation_Should_Fail_When_TenantDoesNotExist()
    {
        // Arrange — handler resolves the tenant via TenantService.GetStatus/Deactivate,
        // which raises NotFoundException for an unknown id.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var missing = $"act-missing-{Guid.NewGuid():N}";

        // Act
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{missing}/activation",
            new { tenantId = missing, isActive = false });

        // Assert
        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Root / Edge Guards

    [Fact]
    public async Task ChangeActivation_Should_Fail_When_DeactivatingRootTenant()
    {
        // Arrange — the root tenant cannot be deactivated (TenantService guard).
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{TestConstants.RootTenantId}/activation",
            new { tenantId = TestConstants.RootTenantId, isActive = false });

        // Assert — guarded; never a 2xx.
        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeActivation_Should_Fail_When_DeactivatingAlreadyDeactivatedTenant()
    {
        // Arrange — deactivate once, then again.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"act-double-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"act-double-{unique}@tenant.com");

        var first = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = false });
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act — deactivate the same tenant again.
        var second = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = false });

        // Assert — already deactivated, so this fails.
        second.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeActivation_Should_Reactivate_When_ProvisioningCompleted()
    {
        // Provision fully (so EnsureCanActivate passes) then deactivate. Reactivation re-checks the latest provisioning
        // record is Completed, which can transiently flip non-terminal after first showing Completed — so wait for *stable* Completed.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"act-reactivate-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"act-reactivate-{unique}@tenant.com");
        await WaitForProvisioningAsync(rootClient, tenantId);
        await WaitForStableCompletedAsync(rootClient, tenantId);

        var deactivate = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = false });
        deactivate.StatusCode.ShouldBe(HttpStatusCode.OK);
        var deactivateResult = await deactivate.Content.ReadFromJsonAsync<LifecycleResult>(Json);
        deactivateResult.ShouldNotBeNull();
        deactivateResult.IsActive.ShouldBeFalse();

        // Act — reactivate (retry while the latest provisioning record settles to
        // Completed; EnsureCanActivate throws a 500 CustomException otherwise).
        HttpResponseMessage reactivate = default!;
        string reactivateBody = string.Empty;
        for (var attempt = 0; attempt < 15; attempt++)
        {
            reactivate = await rootClient.PostAsJsonAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
                new { tenantId, isActive = true });
            if (reactivate.StatusCode == HttpStatusCode.OK)
            {
                break;
            }
            reactivateBody = await reactivate.Content.ReadAsStringAsync();
            await Task.Delay(1000);
        }

        // Assert
        reactivate.StatusCode.ShouldBe(HttpStatusCode.OK, reactivateBody);
        var reactivateResult = await reactivate.Content.ReadFromJsonAsync<LifecycleResult>(Json);
        reactivateResult.ShouldNotBeNull();
        reactivateResult.TenantId.ShouldBe(tenantId);
        reactivateResult.IsActive.ShouldBeTrue();
    }

    #endregion

    #region AuthZ

    [Fact]
    public async Task ChangeActivation_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/anytenant/activation",
            new { tenantId = "anytenant", isActive = false });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Activation {tenantId}",
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

    /// <summary>
    /// Waits until the provisioning status reads "Completed" on several consecutive
    /// polls. Guards against the async-provisioning window where the latest record
    /// transiently reports a non-terminal state right after the first "Completed".
    /// </summary>
    private static async Task WaitForStableCompletedAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        var consecutive = 0;
        for (var i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var status = await statusResponse.Content.ReadFromJsonAsync<ProvisioningSnapshot>(Json);
                if (string.Equals(status?.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    if (++consecutive >= 3)
                    {
                        return;
                    }
                }
                else
                {
                    consecutive = 0;
                }
            }
            await Task.Delay(500);
        }
        // Best-effort: fall through and let the caller's retry handle any residual race.
    }

    private sealed record ProvisioningSnapshot
    {
        public string Status { get; init; } = string.Empty;
    }

    private sealed record LifecycleResult
    {
        public string TenantId { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime? ValidUpto { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    #endregion
}
