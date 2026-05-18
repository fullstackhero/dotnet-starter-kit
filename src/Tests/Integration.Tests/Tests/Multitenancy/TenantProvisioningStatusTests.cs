using System.Text.Json;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Multitenancy;

[Collection(FshCollectionDefinition.Name)]
public sealed class TenantProvisioningStatusTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] ExpectedSteps =
    {
        "Database",
        "Migrations",
        "Seeding",
        "CacheWarm"
    };

    private readonly AuthHelper _auth;

    public TenantProvisioningStatusTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Provisioning_Should_TransitionAllStepsToCompleted_After_TenantCreation()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"prov-{uniqueId}";
        var adminEmail = $"prov-admin-{uniqueId}@tenant.com";

        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Provisioning Test Tenant {uniqueId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var status = await PollUntilTerminalAsync(rootClient, tenantId);

        status.Status.ShouldBe("Completed");
        status.StartedUtc.ShouldNotBeNull();
        status.CompletedUtc.ShouldNotBeNull();
        status.Error.ShouldBeNull();
        status.Steps.Count.ShouldBe(ExpectedSteps.Length);

        foreach (var expectedStep in ExpectedSteps)
        {
            var step = status.Steps.SingleOrDefault(s => s.Step == expectedStep);
            step.ShouldNotBeNull($"Step {expectedStep} missing from provisioning status.");
            step.Status.ShouldBe("Completed", $"Step {expectedStep} did not reach Completed status.");
            step.StartedUtc.ShouldNotBeNull($"Step {expectedStep} has no StartedUtc.");
            step.CompletedUtc.ShouldNotBeNull($"Step {expectedStep} has no CompletedUtc.");
            step.Error.ShouldBeNull();
        }
    }

    private static async Task<ProvisioningStatusDto> PollUntilTerminalAsync(
        HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var response = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<ProvisioningStatusDto>(content, JsonOptions)
                    ?? throw new InvalidOperationException("Failed to deserialize provisioning status.");

                if (status.Status is "Completed" or "Failed")
                {
                    return status;
                }
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not reach a terminal state within {maxRetries} seconds.");
    }

    private sealed record ProvisioningStatusDto(
        string TenantId,
        string Status,
        string CorrelationId,
        string? CurrentStep,
        string? Error,
        DateTime CreatedUtc,
        DateTime? StartedUtc,
        DateTime? CompletedUtc,
        IReadOnlyCollection<ProvisioningStepDto> Steps);

    private sealed record ProvisioningStepDto(
        string Step,
        string Status,
        DateTime? StartedUtc,
        DateTime? CompletedUtc,
        string? Error);
}
