#pragma warning disable S1144 // Unused private members — populated by JSON deserialization
#pragma warning disable S3459 // Unassigned members — populated by JSON deserialization
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Coverage for the retry-provisioning endpoint
/// (<c>POST /api/v1/tenants/{tenantId}/provisioning/retry</c>). A tenant is driven
/// into the <c>Failed</c> state by giving it a syntactically-valid but unreachable
/// connection string (the format-only validator accepts it, then the Migrations step
/// can't connect). Retry must then start a fresh provisioning attempt with a new
/// correlation id. Also covers authentication and the not-found path.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RetryTenantProvisioningTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    // Syntactically valid Postgres connection string pointing at a dead endpoint with a
    // short timeout so the Migrations step fails fast instead of hanging the test.
    private const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=59999;Database=fsh_unreachable;Username=nope;Password=nope;Timeout=2;Command Timeout=2";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public RetryTenantProvisioningTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path (retry of a failed tenant)

    [Fact]
    public async Task RetryProvisioning_Should_StartNewAttempt_When_TenantPreviouslyFailed()
    {
        // Arrange — create a tenant whose provisioning will fail at the Migrations step.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"retry-{unique}";

        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Retry {tenantId}",
            connectionString = UnreachableConnectionString,
            adminEmail = $"retry-{unique}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create failed: {createBody}");

        var failedStatus = await PollUntilStatusAsync(rootClient, tenantId, "Failed");
        failedStatus.Status.ShouldBe("Failed");
        var firstCorrelation = failedStatus.CorrelationId;
        firstCorrelation.ShouldNotBeNullOrEmpty();

        // Act — retry provisioning. A new attempt is started (new correlation id).
        var retryResponse = await rootClient.PostAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning/retry", content: null);

        // Assert — the retry call succeeds and reports a fresh provisioning record.
        retryResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var retryStatus = await retryResponse.Content.ReadFromJsonAsync<ProvisioningStatus>(Json);
        retryStatus.ShouldNotBeNull();
        retryStatus.TenantId.ShouldBe(tenantId);
        retryStatus.CorrelationId.ShouldNotBe(firstCorrelation, "retry must create a new provisioning attempt");
        retryStatus.Steps.Count.ShouldBe(4);
    }

    [Fact]
    public async Task RetryProvisioning_Should_Succeed_When_RetriedAfterCompletion()
    {
        // Arrange — a healthy tenant (null connection string => shared DB) provisions OK.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"retry-ok-{unique}";

        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Retry OK {tenantId}",
            connectionString = (string?)null,
            adminEmail = $"retry-ok-{unique}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var completed = await PollUntilTerminalAsync(rootClient, tenantId);
        completed.Status.ShouldBe("Completed");
        var firstCorrelation = completed.CorrelationId;

        // Act — retry an already-completed tenant.
        var retryResponse = await rootClient.PostAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning/retry", content: null);

        // Assert — accepted and a new attempt is recorded; idempotent steps re-complete.
        retryResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var retryStatus = await retryResponse.Content.ReadFromJsonAsync<ProvisioningStatus>(Json);
        retryStatus.ShouldNotBeNull();
        retryStatus.CorrelationId.ShouldNotBe(firstCorrelation);

        var afterRetry = await PollUntilTerminalAsync(rootClient, tenantId);
        afterRetry.Status.ShouldBe("Completed");
    }

    #endregion

    #region AuthZ / Not Found

    [Fact]
    public async Task RetryProvisioning_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsync(
            $"{TestConstants.TenantsBasePath}/anytenant/provisioning/retry", content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RetryProvisioning_Should_Return404_When_TenantDoesNotExist()
    {
        // Arrange — StartAsync resolves the tenant from the store first; a missing
        // tenant raises NotFoundException => 404.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var missing = $"missing-{Guid.NewGuid():N}";

        // Act
        var response = await rootClient.PostAsync(
            $"{TestConstants.TenantsBasePath}/{missing}/provisioning/retry", content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helpers

    private static async Task<ProvisioningStatus> PollUntilTerminalAsync(
        HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var response = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<ProvisioningStatus>(Json);
                if (status is { Status: "Completed" or "Failed" })
                {
                    return status;
                }
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Tenant {tenantId} provisioning did not reach a terminal state.");
    }

    private static async Task<ProvisioningStatus> PollUntilStatusAsync(
        HttpClient client, string tenantId, string targetStatus, int maxRetries = 90)
    {
        ProvisioningStatus? last = null;
        for (var i = 0; i < maxRetries; i++)
        {
            var response = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (response.IsSuccessStatusCode)
            {
                last = await response.Content.ReadFromJsonAsync<ProvisioningStatus>(Json);
                if (string.Equals(last?.Status, targetStatus, StringComparison.OrdinalIgnoreCase))
                {
                    return last!;
                }
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException(
            $"Tenant {tenantId} did not reach status '{targetStatus}'. Last seen: {last?.Status ?? "<none>"}.");
    }

    private sealed record ProvisioningStatus(
        string TenantId,
        string Status,
        string CorrelationId,
        string? CurrentStep,
        string? Error,
        DateTime CreatedUtc,
        DateTime? StartedUtc,
        DateTime? CompletedUtc,
        IReadOnlyCollection<ProvisioningStep> Steps);

    private sealed record ProvisioningStep(
        string Step, string Status, DateTime? StartedUtc, DateTime? CompletedUtc, string? Error);

    #endregion
}
