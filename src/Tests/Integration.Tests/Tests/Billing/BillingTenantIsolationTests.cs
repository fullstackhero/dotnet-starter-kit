using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
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

    #region Cross-tenant mutation isolation (P0 security regressions)

    // A non-root tenant must never be able to MUTATE another tenant's billing resources by id —
    // the read-side isolation above is necessary but not sufficient. Each test below encodes the
    // exact attack: tenant B (fresh, non-root) targets root's resource. Pre-fix these succeeded.

    [Fact]
    public async Task VoidInvoice_Should_Return404_When_OwnedByDifferentTenant()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month, basePrice: 77.00m);

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var (otherClient, _) = await CreateForeignTenantClientAsync(rootClient, "void");
        using var _o = otherClient;

        // Tenant B tries to void root's invoice → must 404, and the invoice must stay Draft.
        using var crossResp = await otherClient.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{invoiceId}/void", new { reason = "malicious" });
        crossResp.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "a tenant must not be able to void another tenant's invoice by id");

        using var ownerRead = await rootClient.GetAsync($"{BillingBasePath}/invoices/{invoiceId}");
        var dto = await ParseAsync<InvoiceDto>(ownerRead);
        dto.Status.ShouldBe(InvoiceStatus.Draft, "the cross-tenant void attempt must not have mutated the invoice");

        // The owner (root) CAN void it.
        using var ownerVoid = await rootClient.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{invoiceId}/void", new { reason = "ok" });
        ownerVoid.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignSubscription_Should_NotAffect_OtherTenant_When_NonRootPassesForeignTenantId()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("assign");
        await CreatePlanAsync(rootClient, key, name: "Plan-assign", monthlyBasePrice: 9m);
        var rootSubId = await AssignSubscriptionAsync(rootClient, TestConstants.RootTenantId, key);

        var (otherClient, _) = await CreateForeignTenantClientAsync(rootClient, "assign");
        using var _o = otherClient;

        // Tenant B tries to (re)assign ROOT's subscription by passing root's tenant id in the body.
        // The handler must pin B to its own tenant, so root's active subscription is untouched.
        using var resp = await otherClient.PostAsJsonAsync(
            $"{BillingBasePath}/subscriptions", new { tenantId = TestConstants.RootTenantId, planKey = key });

        var rootSubAfter = await GetSubscriptionAsync(rootClient, TestConstants.RootTenantId);
        rootSubAfter.ShouldNotBeNull();
        rootSubAfter!.Id.ShouldBe(rootSubId,
            "a tenant must not be able to cancel/replace root's subscription via a foreign tenant id");
    }

    [Fact]
    public async Task GetUsage_Should_NotLeak_OtherTenants_Snapshots()
    {
        var (year, month) = NextPeriod();
        const long Marker = 918273645; // distinctive usedUnits value to spot a leak in the raw JSON
        await SeedDirectAsync(TestConstants.RootTenantId, async db =>
        {
            db.UsageSnapshots.Add(UsageSnapshot.Capture(
                TestConstants.RootTenantId, year, month, QuotaResource.ApiCalls, usedUnits: Marker, limitUnits: 1000));
            await db.SaveChangesAsync();
        });

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var (otherClient, _) = await CreateForeignTenantClientAsync(rootClient, "usage");
        using var _o = otherClient;
        var markerText = Marker.ToString(CultureInfo.InvariantCulture);

        // B with no filter must not see root's snapshot.
        using var noFilter = await otherClient.GetAsync($"{BillingBasePath}/usage");
        noFilter.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await noFilter.Content.ReadAsStringAsync()).Contains(markerText, StringComparison.Ordinal)
            .ShouldBeFalse("a tenant must not see another tenant's usage snapshots");

        // B explicitly passing root's tenant id must STILL be scoped to B (no bypass).
        using var withRootId = await otherClient.GetAsync($"{BillingBasePath}/usage?tenantId={TestConstants.RootTenantId}");
        withRootId.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await withRootId.Content.ReadAsStringAsync()).Contains(markerText, StringComparison.Ordinal)
            .ShouldBeFalse("passing another tenant's id must not bypass tenant scoping");

        // Root can read its own.
        using var rootRead = await rootClient.GetAsync($"{BillingBasePath}/usage?tenantId={TestConstants.RootTenantId}");
        (await rootRead.Content.ReadAsStringAsync()).Contains(markerText, StringComparison.Ordinal)
            .ShouldBeTrue("the owning tenant must see its own usage snapshot");
    }

    [Fact]
    public async Task CaptureUsage_Should_BeScopedToCaller_When_NonRootPassesForeignTenantId()
    {
        var (year, month) = NextPeriod();
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var (otherClient, _) = await CreateForeignTenantClientAsync(rootClient, "capture");
        using var _o = otherClient;

        // B asks to capture usage FOR ROOT. The handler must pin to B, so nothing it returns/writes
        // belongs to root.
        using var resp = await otherClient.PostAsJsonAsync(
            $"{BillingBasePath}/usage/snapshots/capture",
            new { tenantId = TestConstants.RootTenantId, periodYear = year, periodMonth = month });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());

        var snaps = await ParseAsync<List<UsageSnapshotDto>>(resp);
        snaps.ShouldAllBe(s => s.TenantId != TestConstants.RootTenantId,
            "a tenant capturing usage must be scoped to itself — never able to write another tenant's usage");
    }

    [Fact]
    public async Task GenerateInvoices_Should_BeForbidden_For_NonRootTenant()
    {
        var (year, month) = NextPeriod();
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var (otherClient, _) = await CreateForeignTenantClientAsync(rootClient, "generate");
        using var _o = otherClient;

        using var crossResp = await otherClient.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/generate", new { periodYear = year, periodMonth = month });
        crossResp.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "platform-wide invoice generation must be root-operator only");

        using var rootResp = await rootClient.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/generate", new { periodYear = year, periodMonth = month });
        rootResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Usage-invoice generation correctness (P1)

    [Fact]
    public async Task GenerateInvoiceForPeriod_Should_CreateUsageInvoice_EvenWhen_SubscriptionInvoiceSharesThePeriod()
    {
        // Reproduces the revenue bug: the idempotency check used to match ANY invoice for the period
        // (no Purpose), so when a Subscription invoice already existed for the month the Usage/overage
        // invoice was silently skipped. Seed an active subscription + a Subscription invoice, then
        // generate — a Usage invoice must still be produced.
        var (year, month) = NextPeriod();
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("usgskip");
        await CreatePlanAsync(rootClient, key, name: "Plan-usgskip", monthlyBasePrice: 15m);
        await AssignSubscriptionAsync(rootClient, TestConstants.RootTenantId, key);

        await SeedDirectAsync(TestConstants.RootTenantId, async db =>
        {
            var subInvoice = Invoice.CreateDraft(
                TestConstants.RootTenantId,
                $"SUB-SEED-{year}{month:00}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                year, month, "USD", InvoicePurpose.Subscription, periodStartUtc: null, periodEndUtc: null);
            subInvoice.AddLineItem(InvoiceLineItemKind.BaseFee, "Seeded subscription fee", 1m, 15m);
            db.Invoices.Add(subInvoice);
            await db.SaveChangesAsync();
        });

        // Invoke the generator directly for this exact period (tenant context = root). Pre-fix the
        // idempotency check matched the seeded SUBSCRIPTION invoice and returned it (Purpose=Subscription),
        // skipping usage billing. Post-fix it must produce a USAGE invoice.
        var generated = await InvokeGenerateInvoiceForPeriodAsync(TestConstants.RootTenantId, year, month);

        generated.ShouldNotBeNull("the generator must produce an invoice when an active subscription exists");
        generated!.Purpose.ShouldBe(InvoicePurpose.Usage,
            "the usage invoice must be generated even when a subscription invoice already shares the month");
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

    /// <summary>Provisions a fresh non-root tenant and returns an authenticated admin client for it.</summary>
    private async Task<(HttpClient Client, string TenantId)> CreateForeignTenantClientAsync(HttpClient rootClient, string label)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"billing-{label}-iso-{uniqueId}";
        var adminEmail = $"billing-{label}-admin-{uniqueId}@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);
        var client = await CreateTenantAdminClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);
        return (client, tenantId);
    }

    /// <summary>Invokes the billing generator for one tenant/period directly, under that tenant's context.</summary>
    private async Task<Invoice?> InvokeGenerateInvoiceForPeriodAsync(string tenantId, int year, int month)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        var billing = scope.ServiceProvider.GetRequiredService<FSH.Modules.Billing.Services.IBillingService>();
        return await billing.GenerateInvoiceForPeriodAsync(tenantId, year, month);
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
