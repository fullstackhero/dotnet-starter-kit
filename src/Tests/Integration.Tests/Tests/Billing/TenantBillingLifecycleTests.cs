#pragma warning disable S1144, S3459 // members populated by JSON deserialization
using System.Text.Json;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.Dtos;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// End-to-end coverage for the tenant → billing wiring: creating a tenant with a plan must (via the
/// TenantSubscribed integration event, dispatched synchronously on the in-memory bus) start an active
/// subscription and issue a Subscription-purpose invoice for the plan term, and set the tenant's
/// validity from the plan interval. A free (zero-price) plan creates the subscription but no invoice.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TenantBillingLifecycleTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly AuthHelper _auth;

    public TenantBillingLifecycleTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task CreateTenant_With_Paid_Plan_Should_Start_Subscription_And_Issue_Invoice()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var planKey = await CreatePlanAsync(rootClient, $"life-{unique}", monthlyBasePrice: 20m);
        var tenantId = $"life-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"life-{unique}@tenant.com", planKey);

        // Active subscription on the new plan.
        var subscription = await rootClient.GetFromJsonAsync<SubscriptionDto>(
            $"{BillingBasePath}/subscriptions?tenantId={tenantId}", Json);
        subscription.ShouldNotBeNull("creating a tenant must start a subscription");
        subscription!.PlanKey.ShouldBe(planKey);
        subscription.Status.ShouldBe(SubscriptionStatus.Active);

        // Exactly one issued subscription invoice for the plan term price.
        var invoices = await GetInvoicesAsync(rootClient, tenantId);
        var subInvoices = invoices.Where(i => i.Purpose == InvoicePurpose.Subscription).ToList();
        subInvoices.Count.ShouldBe(1, "the plan term base fee must be invoiced once on create");
        subInvoices[0].Status.ShouldBe(InvoiceStatus.Issued);
        subInvoices[0].SubtotalAmount.ShouldBe(20m);

        // Validity set from the monthly plan term (~1 month out).
        var status = await rootClient.GetFromJsonAsync<TenantStatus>(
            $"{TestConstants.TenantsBasePath}/{tenantId}/status", Json);
        status.ShouldNotBeNull();
        status!.Plan.ShouldBe(planKey);
        status.ValidUpto!.Value.ShouldBeGreaterThan(DateTime.UtcNow.AddDays(27));
        status.ValidUpto.Value.ShouldBeLessThan(DateTime.UtcNow.AddDays(32));
    }

    [Fact]
    public async Task CreateTenant_With_Free_Plan_Should_Start_Subscription_Without_Invoice()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var planKey = await CreatePlanAsync(rootClient, $"free-{unique}", monthlyBasePrice: 0m);
        var tenantId = $"free-{unique}";
        await CreateTenantAsync(rootClient, tenantId, $"free-{unique}@tenant.com", planKey);

        var subscription = await rootClient.GetFromJsonAsync<SubscriptionDto>(
            $"{BillingBasePath}/subscriptions?tenantId={tenantId}", Json);
        subscription.ShouldNotBeNull("a free plan still gets a subscription");
        subscription!.Status.ShouldBe(SubscriptionStatus.Active);

        var invoices = await GetInvoicesAsync(rootClient, tenantId);
        invoices.Any(i => i.Purpose == InvoicePurpose.Subscription)
            .ShouldBeFalse("a zero-price plan must not produce a subscription invoice");
    }

    private static async Task<IReadOnlyCollection<InvoiceDto>> GetInvoicesAsync(HttpClient client, string tenantId)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<InvoiceDto>>(
            $"{BillingBasePath}/invoices?tenantId={tenantId}&pageNumber=1&pageSize=50", Json);
        page.ShouldNotBeNull();
        return page!.Items;
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
            name = $"Life {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
            planKey,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    private sealed record TenantStatus
    {
        public string Id { get; init; } = string.Empty;
        public DateTime? ValidUpto { get; init; }
        public string? Plan { get; init; }
        public string ExpiryState { get; init; } = "Active";
    }
}
