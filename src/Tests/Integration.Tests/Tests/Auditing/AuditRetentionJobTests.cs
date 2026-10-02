using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Persistence;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// The audit retention purge is a Hangfire recurring job registered without a tenant parameter, so it
/// runs with no ambient tenant. AuditRecords carry the default-on tenant filter, which means the purge
/// only reaches a tenant's rows when it runs inside that tenant's context. Records are seeded in two
/// tenants and the job is resolved from a fresh, tenant-less scope — the same shape Hangfire's
/// activator gives it — and run once.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AuditRetentionJobTests
{
    // Activity events are kept for 30 days by default; 31 is safely past the cutoff, 1 is inside it.
    private const int ActivityRetentionDays = 30;
    private const int DaysPastRetention = 31;
    private const int DaysInsideRetention = 1;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AuditRetentionJobTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task RunAsync_Should_PurgeRecordsPastRetention_InEveryTenant_And_KeepTheRest()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"audit-ret-{uniqueId}";
        var otherAdminEmail = $"audit-ret-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);

        var now = DateTime.UtcNow;
        var rootOld = await SeedAuditAsync(TestConstants.RootTenantId, now.AddDays(-DaysPastRetention));
        var otherOld = await SeedAuditAsync(otherTenantId, now.AddDays(-DaysPastRetention));
        var rootRecent = await SeedAuditAsync(TestConstants.RootTenantId, now.AddDays(-DaysInsideRetention));
        var otherRecent = await SeedAuditAsync(otherTenantId, now.AddDays(-DaysInsideRetention));

        var options = new AuditRetentionOptions
        {
            Enabled = true,
            ActivityRetentionDays = ActivityRetentionDays,
        };

        // Act — a fresh scope with no tenant set, as FshJobActivator gives a job registered via AddOrUpdate.
        using (var jobScope = _factory.Services.CreateScope())
        {
            var job = ActivatorUtilities.CreateInstance<AuditRetentionJob>(jobScope.ServiceProvider, options);
            await job.RunAsync(CancellationToken.None);
        }

        // Assert
        (await AuditExistsAsync(TestConstants.RootTenantId, rootOld))
            .ShouldBeFalse("a root-tenant audit record past retention must be purged");
        (await AuditExistsAsync(otherTenantId, otherOld))
            .ShouldBeFalse("an audit record past retention in a non-root tenant must be purged");
        (await AuditExistsAsync(TestConstants.RootTenantId, rootRecent))
            .ShouldBeTrue("a root-tenant audit record inside retention must be kept");
        (await AuditExistsAsync(otherTenantId, otherRecent))
            .ShouldBeTrue("an audit record inside retention in a non-root tenant must be kept");
    }

    // Tenant context is an AsyncLocal, so it is set in the same method as the DbContext call.
    private async Task<Guid> SeedAuditAsync(string tenantId, DateTime occurredAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(tenantId);
        tenant.ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var record = new AuditRecord
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = occurredAtUtc,
            ReceivedAtUtc = occurredAtUtc,
            EventType = (int)AuditEventType.Activity,
            Severity = (byte)AuditSeverity.Information,
            TenantId = tenantId,
            Source = "audit-retention-test",
            PayloadJson = "{}",
        };

        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        db.AuditRecords.Add(record);
        await db.SaveChangesAsync();
        return record.Id;
    }

    private async Task<bool> AuditExistsAsync(string tenantId, Guid auditId)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(tenantId);
        tenant.ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        return await db.AuditRecords.AsNoTracking().AnyAsync(a => a.Id == auditId);
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

    // The status body also lists each step, and a finished step reads "Completed" while later steps
    // are still running, so only the overall Status field is trusted.
    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId)
    {
        const int maxRetries = 60;
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                var status = await statusResponse.Content.ReadFromJsonAsync<TenantProvisioningStatusDto>();
                if (string.Equals(status?.Status, "Completed", StringComparison.Ordinal))
                {
                    return;
                }

                if (string.Equals(status?.Status, "Failed", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed at {status?.CurrentStep}: {status?.Error}");
                }
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds.");
    }
}
