using System.Text.Json;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Multitenancy.Provisioning;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Failure-path coverage for tenant provisioning (master test plan P0 #5). The happy path is
/// proven by <see cref="TenantProvisioningStatusTests"/>; here we prove the inverse:
/// when a provisioning step throws, the overall status transitions to <c>Failed</c> with the
/// failing step recorded, and the tenant cannot be activated while status != <c>Completed</c>.
///
/// Fault-injection seam (option a — a legitimate input that fails a step end-to-end):
/// the create endpoint accepts any *well-formed* connection string (the
/// <c>ConnectionStringValidator</c> only parses it, it does not probe reachability). A
/// connection string pointed at a port nothing listens on (127.0.0.1:1) is therefore accepted,
/// persisted on the tenant, and — because <c>BaseDbContext.OnConfiguring</c> routes a tenant
/// with a non-empty connection string to that DB — the Migrations step's
/// <c>GetPendingMigrationsAsync</c> throws a real Npgsql connection failure. The
/// <c>TenantProvisioningJob</c> catch block converts that to
/// <c>MarkFailedAsync(..., Migrations, ...)</c>. No production code or shared test infra is
/// modified; the failure flows through the exact production pipeline.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TenantProvisioningFailureTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Well-formed (passes ConnectionStringValidator) but unreachable: port 1 refuses
    // connections immediately, and the short timeouts keep the failing job fast.
    private const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=does_not_exist;Username=postgres;Password=x;Timeout=3;Command Timeout=3";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TenantProvisioningFailureTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Failure Path

    [Fact]
    public async Task Provisioning_Should_TransitionToFailed_When_MigrationsStepThrows()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"fail-{uniqueId}";

        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Provisioning Failure Tenant {uniqueId}",
            connectionString = UnreachableConnectionString,
            adminEmail = $"fail-admin-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act
        var status = await PollUntilTerminalAsync(rootClient, tenantId);

        // Assert — overall provisioning failed and the failure is attributed to a step.
        status.Status.ShouldBe("Failed");
        status.CompletedUtc.ShouldNotBeNull();
        status.Error.ShouldNotBeNullOrWhiteSpace();
        status.CurrentStep.ShouldNotBeNull();

        // The Database step (no DB I/O) completes; Migrations is where the bad
        // connection string first opens a connection, so it is the failing step.
        var migrationsStep = status.Steps.SingleOrDefault(s => s.Step == "Migrations");
        migrationsStep.ShouldNotBeNull("Migrations step missing from provisioning status.");
        migrationsStep.Status.ShouldBe("Failed");
        migrationsStep.Error.ShouldNotBeNullOrWhiteSpace();
        status.CurrentStep.ShouldBe("Migrations");

        // Steps after the failure must never have completed.
        var seedingStep = status.Steps.SingleOrDefault(s => s.Step == "Seeding");
        seedingStep.ShouldNotBeNull();
        seedingStep.Status.ShouldNotBe("Completed");
    }

    #endregion

    #region Activation Blocked While Not Completed

    [Fact]
    public async Task EnsureCanActivate_Should_Throw_When_ProvisioningStatusIsFailed()
    {
        // Arrange — create a tenant whose provisioning fails on the Migrations step.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"failguard-{uniqueId}";

        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Activation Guard Tenant {uniqueId}",
            connectionString = UnreachableConnectionString,
            adminEmail = $"failguard-admin-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var status = await PollUntilTerminalAsync(rootClient, tenantId);
        status.Status.ShouldBe("Failed");

        // Act + Assert — drive the production activation guard directly. The Finbuckle tenant
        // context for TenantDbContext access is set inline in this method (AsyncLocal — must not
        // cross an awaited helper) to satisfy the named tenant query filter.
        using var scope = _factory.Services.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();

        var ex = await Should.ThrowAsync<CustomException>(
            () => provisioningService.EnsureCanActivateAsync(tenantId, CancellationToken.None));

        ex.Message.ShouldContain(tenantId);
        ex.Message.ShouldContain("Failed");
    }

    [Fact]
    public async Task ReactivateTenant_Should_BeRejected_When_ProvisioningFailed()
    {
        // Arrange — a tenant that fails provisioning. It is created active, so we must first
        // deactivate it, then prove reactivation is blocked by the provisioning guard.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"reactivate-{uniqueId}";

        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Reactivate Blocked Tenant {uniqueId}",
            connectionString = UnreachableConnectionString,
            adminEmail = $"reactivate-admin-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var status = await PollUntilTerminalAsync(rootClient, tenantId);
        status.Status.ShouldBe("Failed");

        // Deactivate succeeds (root + this tenant keep the active-tenant count > 1).
        var deactivateResponse = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = false });
        deactivateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act — attempt reactivation; ActivateAsync calls EnsureCanActivateAsync, which throws
        // because the latest provisioning status is Failed (not Completed).
        var reactivateResponse = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = true });

        // Assert — reactivation is rejected and the tenant remains inactive.
        reactivateResponse.IsSuccessStatusCode.ShouldBeFalse();

        var statusResponse = await rootClient.GetAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/status");
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statusBody = await statusResponse.Content.ReadAsStringAsync();
        var tenantStatus = JsonSerializer.Deserialize<TenantStatusProbe>(statusBody, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize tenant status.");
        tenantStatus.IsActive.ShouldBeFalse();
    }

    #endregion

    #region Helpers

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

    private sealed record TenantStatusProbe(string Id, bool IsActive);

    #endregion
}
