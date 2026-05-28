using System.Text;
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// Coverage for the invoice PDF download endpoint (<c>GET /api/v1/billing/invoices/{id}/pdf</c>):
/// a tenant can download its own invoice as application/pdf, and the fetch is scoped to the caller's
/// tenant so another tenant's invoice id resolves to 404 (no cross-tenant leak).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class InvoicePdfTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private readonly AuthHelper _auth;

    public InvoicePdfTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Tenant_Should_Download_Own_Invoice_AsPdf_And_NotAnotherTenants()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"pdf-{unique}";
        var adminEmail = $"pdf-{unique}@tenant.com";
        var planKey = await CreatePlanAsync(rootClient, $"pdf-m-{unique}", 29m);
        await CreateTenantAsync(rootClient, tenantId, adminEmail, planKey);
        await WaitForProvisioningAsync(rootClient, tenantId);

        // The subscription invoice was issued on create; grab its id from the operator list.
        var invoiceId = await GetFirstInvoiceIdAsync(rootClient, tenantId);

        using var tenantClient = await CreateTenantAdminClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);

        // Own invoice → 200 application/pdf with a real PDF body.
        using var ownResponse = await tenantClient.GetAsync($"{BillingBasePath}/invoices/{invoiceId}/pdf");
        ownResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await ownResponse.Content.ReadAsStringAsync());
        ownResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
        var bytes = await ownResponse.Content.ReadAsByteArrayAsync();
        bytes.Length.ShouldBeGreaterThan(0);
        Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("%PDF");

        // Cross-tenant: the root operator's own context has no such invoice → 404 (no leak).
        using var crossResponse = await rootClient.GetAsync($"{BillingBasePath}/invoices/{invoiceId}/pdf");
        crossResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<string> GetFirstInvoiceIdAsync(HttpClient client, string tenantId)
    {
        var resp = await client.GetAsync($"{BillingBasePath}/invoices?tenantId={tenantId}&pageNumber=1&pageSize=50");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("items");
        items.GetArrayLength().ShouldBeGreaterThan(0, "the paid-plan tenant must have a subscription invoice");
        return items[0].GetProperty("id").GetString()!;
    }

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
            name = $"Pdf {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
            planKey,
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
}
