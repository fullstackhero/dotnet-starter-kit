using FSH.Framework.Mailing.Services;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Domain;
using FSH.Modules.Multitenancy.Services;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Coverage for the daily tenant-expiry notification pipeline: the scan job records a dedup notice and
/// publishes the matching event (which emails the tenant admin), and re-running the scan does not
/// re-notify the same state for the same validity period. Also covers the invoice-issued email.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TenantExpiryScanJobTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TenantExpiryScanJobTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ScanJob_Should_Record_GraceNotice_Dedup_And_Email()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"scan-{unique}";
        var adminEmail = $"scan-{unique}@tenant.com";
        var planKey = await CreatePlanAsync(rootClient, $"scan-m-{unique}", 10m);
        await CreateTenantAsync(rootClient, tenantId, adminEmail, planKey);

        // Lapse into the grace period (1 day past ValidUpto).
        var adjust = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/adjust-validity",
            new { tenantId, validUpto = DateTime.UtcNow.AddDays(-1) });
        adjust.StatusCode.ShouldBe(HttpStatusCode.OK, await adjust.Content.ReadAsStringAsync());

        var mail = (NoOpMailService)_factory.Services.GetRequiredService<IMailService>();
        mail.Clear();

        await RunScanAsync();
        (await CountNoticesAsync(tenantId, TenantExpiryNoticeTypes.EnteredGrace))
            .ShouldBe(1, "the first scan must record one grace notice");

        // Dedup: a second scan must not re-notify the same state for the same validity period.
        await RunScanAsync();
        (await CountNoticesAsync(tenantId, TenantExpiryNoticeTypes.EnteredGrace))
            .ShouldBe(1, "the scan must not re-notify the same state for the same validity period");

        mail.Sent.ShouldContain(
            m => m.To.Contains(adminEmail) && m.Subject.Contains("grace", StringComparison.OrdinalIgnoreCase),
            "the grace notice must email the tenant admin");
    }

    [Fact]
    public async Task InvoiceIssued_Should_Email_TenantAdmin_OnPaidPlan()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"inv-{unique}";
        var adminEmail = $"inv-{unique}@tenant.com";
        var planKey = await CreatePlanAsync(rootClient, $"inv-m-{unique}", 19m);

        var mail = (NoOpMailService)_factory.Services.GetRequiredService<IMailService>();
        mail.Clear();

        // Creating a tenant on a paid plan issues the subscription invoice synchronously, which emails the admin.
        await CreateTenantAsync(rootClient, tenantId, adminEmail, planKey);

        mail.Sent.ShouldContain(
            m => m.To.Contains(adminEmail) && m.Subject.Contains("Invoice", StringComparison.OrdinalIgnoreCase),
            "issuing the subscription invoice must email the tenant admin");
    }

    private async Task RunScanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TenantExpiryScanJob>();
        await job.RunAsync(CancellationToken.None);
    }

    private async Task<int> CountNoticesAsync(string tenantId, string noticeType)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        return await db.TenantExpiryNotices.CountAsync(x => x.TenantId == tenantId && x.NoticeType == noticeType);
    }

    private static async Task<string> CreatePlanAsync(HttpClient client, string key, decimal monthlyBasePrice)
    {
        var resp = await client.PostAsJsonAsync($"{BillingBasePath}/plans",
            new { key, name = $"Plan {key}", currency = "USD", monthlyBasePrice });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        return key;
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail, string planKey)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Scan {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
            planKey,
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }
}
