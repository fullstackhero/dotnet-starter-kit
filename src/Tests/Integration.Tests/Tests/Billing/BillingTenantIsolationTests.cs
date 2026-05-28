using System.Text.Json;
using System.Text.Json.Serialization;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// Cross-TENANT direct fetch-by-id isolation for the Billing module. The list endpoints already
/// prove "my invoices/subscription only" (see BillingEndpointTests). This class closes the
/// remaining gap: fetching a billing resource that belongs to ANOTHER tenant by its id must
/// behave as if the resource does not exist (404 / empty) — never a leak — while the OWNING
/// tenant fetches the exact same id successfully.
///
/// Tenant A is <c>root</c> (the owner). Tenant A's admin creates a fresh tenant B, waits for
/// provisioning, and authenticates as B's admin. B then attempts the cross-tenant fetches.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class BillingTenantIsolationTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Seeded invoices share the (TenantId, PeriodYear, PeriodMonth) unique index with
    // BillingEndpointTests' rows. A process-wide counter keeps every period distinct; years start
    // at 2090 to stay clear of that class's 2080-based range.
    private static int s_periodCounter;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public BillingTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Invoice fetch-by-id isolation

    [Fact]
    public async Task GetInvoiceById_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Arrange — root (tenant A) owns a draft invoice; a fresh tenant B is the attacker.
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month, basePrice: 42.50m);

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"billing-inv-iso-{uniqueId}";
        var otherAdminEmail = $"billing-inv-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Act + Assert — owner can read its own invoice by id.
        using var ownerResponse = await rootClient.GetAsync($"{BillingBasePath}/invoices/{invoiceId}");
        ownerResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            "the owning tenant must be able to fetch its own invoice by id");
        var ownerDto = await ParseAsync<InvoiceDto>(ownerResponse);
        ownerDto.Id.ShouldBe(invoiceId);
        ownerDto.TenantId.ShouldBe(TestConstants.RootTenantId);

        // Act + Assert — a different tenant fetching the SAME id must see a 404, not a leak.
        using var crossResponse = await otherClient.GetAsync($"{BillingBasePath}/invoices/{invoiceId}");
        crossResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "cross-tenant fetch-by-id must return 404, never the other tenant's invoice");

        // The 404 body must not leak the other tenant's invoice data. (The caller-supplied id may
        // appear in a "not found" message — that is not a leak — but private fields must not.)
        var crossBody = await crossResponse.Content.ReadAsStringAsync();
        crossBody.ShouldNotContain("42.50");
        crossBody.ShouldNotContain("\"tenantId\":\"root\"");
        crossBody.ShouldNotContain("Seeded base fee");
    }

    #endregion

    #region Subscription fetch isolation

    [Fact]
    public async Task GetSubscription_Should_NotLeak_When_OtherTenantPassesOwnersTenantId()
    {
        // Arrange — root (tenant A) holds an active subscription; a fresh tenant B is the attacker.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("iso");
        await CreatePlanAsync(rootClient, key, name: "Plan-iso", monthlyBasePrice: 11m);
        var rootSubId = await AssignSubscriptionAsync(rootClient, TestConstants.RootTenantId, key);

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"billing-sub-iso-{uniqueId}";
        var otherAdminEmail = $"billing-sub-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Act + Assert — owner reads its own subscription back.
        var ownerSub = await GetSubscriptionAsync(rootClient, TestConstants.RootTenantId);
        ownerSub.ShouldNotBeNull("the owning tenant must be able to read its own subscription");
        ownerSub!.Id.ShouldBe(rootSubId);
        ownerSub.TenantId.ShouldBe(TestConstants.RootTenantId);

        // Act + Assert — tenant B passing root's tenant id must NOT receive root's subscription.
        // The global tenant filter scopes the query to B, so the (TenantId == "root") predicate
        // matches nothing and the endpoint returns an empty/null body — not a leak.
        using var crossResponse = await otherClient.GetAsync(
            $"{BillingBasePath}/subscriptions?tenantId={TestConstants.RootTenantId}");
        crossResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var crossBody = await crossResponse.Content.ReadAsStringAsync();

        // A different tenant must not be able to read root's subscription by passing root's tenant id.
        crossBody.ShouldNotContain(rootSubId.ToString());

        // The attacking tenant has its own (auto-provisioned) subscription, so the fetch may return
        // THAT — but never root's. The isolation guarantee is "you never see another tenant's row".
        var crossSub = await GetSubscriptionAsync(otherClient, TestConstants.RootTenantId);
        crossSub?.Id.ShouldNotBe(rootSubId,
            "cross-tenant subscription fetch must never resolve to root's subscription");
        crossSub?.TenantId.ShouldNotBe(TestConstants.RootTenantId,
            "cross-tenant subscription fetch must never resolve to root's subscription");
    }

    #endregion

    #region helpers

    /// <summary>Plan keys are lowercased + unique-indexed; this helper guarantees no collisions.</summary>
    private static string UniqueKey(string prefix) =>
        $"plan-iso-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>
    /// Returns a (Year, Month) that no other test in this class has used yet. Years start at 2090
    /// to keep this class's seeded invoices clear of BillingEndpointTests' 2080-based periods on
    /// the shared (TenantId, PeriodYear, PeriodMonth) unique index.
    /// </summary>
    private static (int Year, int Month) NextPeriod()
    {
        int n = Interlocked.Increment(ref s_periodCounter);
        int yearOffset = (n - 1) / 12;
        int month = ((n - 1) % 12) + 1;
        return (2090 + yearOffset, month);
    }

    private static async Task<T> ParseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Expected success status, got {(int)response.StatusCode} {response.StatusCode}. Body: {json}");
        }
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}. Body: {json}");
    }

    private static async Task<Guid> CreatePlanAsync(
        HttpClient client, string key, string name, decimal monthlyBasePrice, string currency = "USD")
    {
        using var response = await client.PostAsJsonAsync($"{BillingBasePath}/plans",
            new { key, name, currency, monthlyBasePrice });
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"CreatePlan failed: {response.StatusCode}\n{body}");
        }
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> AssignSubscriptionAsync(HttpClient client, string tenantId, string planKey)
    {
        using var response = await client.PostAsJsonAsync($"{BillingBasePath}/subscriptions",
            new { tenantId, planKey });
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"AssignSubscription failed: {response.StatusCode}\n{body}");
        }
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<SubscriptionDto?> GetSubscriptionAsync(HttpClient client, string tenantId)
    {
        using var response = await client.GetAsync($"{BillingBasePath}/subscriptions?tenantId={tenantId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return null;
        }
        return JsonSerializer.Deserialize<SubscriptionDto>(json, JsonOptions);
    }

    private async Task<Guid> SeedDraftInvoiceAsync(string tenantId, int year, int month, decimal basePrice = 10m)
    {
        Guid id = Guid.Empty;
        await SeedDirectAsync(tenantId, async db =>
        {
            var invoiceNumber = $"INV-ISO-{year}{month:00}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            var inv = Invoice.CreateDraft(tenantId, invoiceNumber, year, month, "USD");
            inv.AddLineItem(InvoiceLineItemKind.BaseFee, "Seeded base fee", 1m, basePrice);
            db.Invoices.Add(inv);
            await db.SaveChangesAsync();
            id = inv.Id;
        });
        return id;
    }

    // The Finbuckle tenant context is an AsyncLocal; it MUST be set inline in the same method body
    // that resolves and uses the DbContext, otherwise it is lost across the await boundary and the
    // tenant query filter throws a NullReferenceException.
    private async Task SeedDirectAsync(string tenantId, Func<BillingDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(tenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await action(db);
    }

    // ─── cross-tenant scaffolding (mirrors WebhookTenantIsolationTests) ──────────────────────

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
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
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed: {content}");
                }
            }

            await Task.Delay(1000);
        }

        var finalResponse = await client.GetAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
        var finalContent = finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadAsStringAsync()
            : $"HTTP {finalResponse.StatusCode}";

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. Last status: {finalContent}");
    }

    #endregion
}
