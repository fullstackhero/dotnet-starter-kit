using System.Text.Json;
using System.Text.Json.Serialization;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// End-to-end coverage for the Billing module endpoints beyond the existing UsageMeteringTests:
/// Plans CRUD, Subscriptions assign + lookup, and the Invoice lifecycle (Generate → Issue → Pay /
/// Void). Invoice state-machine tests seed Draft invoices directly via the DbContext so each test
/// can exercise its own transition without depending on Generate setup.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class BillingEndpointTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Tests in this class need unique billing periods to avoid colliding on the
    // (TenantId, PeriodYear, PeriodMonth) unique index. The shared factory is reused across the
    // collection, so a process-wide counter keeps every test's period distinct.
    private static int s_periodCounter;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public BillingEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── Plans ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePlan_Should_Return200_And_Persist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("create");

        using var response = await client.PostAsJsonAsync($"{BillingBasePath}/plans",
            new { key, name = "Plan-create", currency = "USD", monthlyBasePrice = 9.99m });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var planId = await response.DeserializeAsync<Guid>();
        planId.ShouldNotBe(Guid.Empty);

        var plans = await GetPlansAsync(client);
        plans.ShouldContain(p =>
            p.Id == planId
            && string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)
            && p.MonthlyBasePrice == 9.99m);
    }

    [Fact]
    public async Task GetPlans_Should_Return_NewlyCreated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("list");
        var planId = await CreatePlanAsync(client, key, name: "Plan-list", monthlyBasePrice: 1m);

        var plans = await GetPlansAsync(client);
        plans.ShouldContain(p => p.Id == planId);
    }

    [Fact]
    public async Task GetPlans_Default_Should_Exclude_Inactive_Plans()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("inactive");
        var planId = await CreatePlanAsync(client, key, name: "Plan-inactive", monthlyBasePrice: 1m);

        // No public Deactivate endpoint — flip IsActive directly via the DbContext.
        await SeedDirectAsync(async db =>
        {
            var plan = await db.Plans.FindAsync(planId);
            plan.ShouldNotBeNull();
            plan!.Deactivate();
            await db.SaveChangesAsync();
        });

        var defaultList = await GetPlansAsync(client);
        defaultList.ShouldNotContain(p => p.Id == planId,
            "GetPlans without includeInactive must exclude inactive plans");

        var inclusiveList = await GetPlansAsync(client, includeInactive: true);
        inclusiveList.ShouldContain(p => p.Id == planId && !p.IsActive,
            "GetPlans?includeInactive=true must surface the deactivated plan with IsActive=false");
    }

    [Fact]
    public async Task UpdatePlan_Should_Persist_Name_And_Price()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("update");
        var planId = await CreatePlanAsync(client, key, name: "Old name", monthlyBasePrice: 5m);

        using var response = await client.PutAsJsonAsync($"{BillingBasePath}/plans/{planId}",
            new { planId, name = "New name", monthlyBasePrice = 25.50m });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var plans = await GetPlansAsync(client);
        var updated = plans.Single(p => p.Id == planId);
        updated.Name.ShouldBe("New name");
        updated.MonthlyBasePrice.ShouldBe(25.50m);
    }

    [Fact]
    public async Task UpdatePlan_Should_Return404_When_PlanDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var unknownId = Guid.NewGuid();

        using var response = await client.PutAsJsonAsync($"{BillingBasePath}/plans/{unknownId}",
            new { planId = unknownId, name = "Ghost", monthlyBasePrice = 1m });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePlan_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsJsonAsync($"{BillingBasePath}/plans",
            new { key = UniqueKey("unauth"), name = "x", currency = "USD", monthlyBasePrice = 1m });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPlans_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync($"{BillingBasePath}/plans");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── Subscriptions ───────────────────────────────────────────────

    [Fact]
    public async Task AssignSubscription_Should_Return200_And_Persist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("sub-basic");
        await CreatePlanAsync(client, key, name: "Plan-sub-basic", monthlyBasePrice: 1m);

        using var response = await client.PostAsJsonAsync($"{BillingBasePath}/subscriptions",
            new { tenantId = TestConstants.RootTenantId, planKey = key });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var subscriptionId = await response.DeserializeAsync<Guid>();
        subscriptionId.ShouldNotBe(Guid.Empty);

        var current = await GetSubscriptionAsync(client, TestConstants.RootTenantId);
        current.ShouldNotBeNull();
        current!.Id.ShouldBe(subscriptionId);
        current.PlanKey.ShouldBe(key, StringCompareShould.IgnoreCase);
        current.Status.ShouldBe(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task AssignSubscription_Should_Replace_Existing_Active_Subscription()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var firstKey = UniqueKey("first");
        var secondKey = UniqueKey("second");
        await CreatePlanAsync(client, firstKey, name: "First", monthlyBasePrice: 1m);
        await CreatePlanAsync(client, secondKey, name: "Second", monthlyBasePrice: 2m);

        var firstSubId = await AssignSubscriptionAsync(client, TestConstants.RootTenantId, firstKey);
        var secondSubId = await AssignSubscriptionAsync(client, TestConstants.RootTenantId, secondKey);
        secondSubId.ShouldNotBe(firstSubId);

        var current = await GetSubscriptionAsync(client, TestConstants.RootTenantId);
        current.ShouldNotBeNull();
        current!.Id.ShouldBe(secondSubId, "the second assign must become the active subscription");
        current.PlanKey.ShouldBe(secondKey, StringCompareShould.IgnoreCase);

        // First should be cancelled, not active — visible only through direct DB inspection.
        await SeedDirectAsync(async db =>
        {
            var oldSub = await db.Subscriptions.FindAsync(firstSubId);
            oldSub.ShouldNotBeNull();
            oldSub!.Status.ShouldBe(SubscriptionStatus.Cancelled,
                "first subscription must be cancelled after the second one is assigned");
        });
    }

    [Fact]
    public async Task AssignSubscription_Should_Return404_When_PlanKey_Unknown()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync($"{BillingBasePath}/subscriptions",
            new { tenantId = TestConstants.RootTenantId, planKey = "plan-does-not-exist-" + Guid.NewGuid().ToString("N")[..6] });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMySubscription_Should_Return_Current_Tenant_Subscription()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("mine");
        await CreatePlanAsync(client, key, name: "Plan-mine", monthlyBasePrice: 1m);
        var subId = await AssignSubscriptionAsync(client, TestConstants.RootTenantId, key);

        using var response = await client.GetAsync($"{BillingBasePath}/subscriptions/me");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await ParseAsync<SubscriptionDto?>(response);
        dto.ShouldNotBeNull();
        dto!.Id.ShouldBe(subId);
        dto.TenantId.ShouldBe(TestConstants.RootTenantId);
    }

    [Fact]
    public async Task AssignSubscription_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsJsonAsync($"{BillingBasePath}/subscriptions",
            new { tenantId = TestConstants.RootTenantId, planKey = "free" });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── Invoices: queries ───────────────────────────────────────────

    [Fact]
    public async Task GetInvoiceById_Should_Return_Seeded_Invoice_With_Expected_Shape()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month, basePrice: 19.99m);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync($"{BillingBasePath}/invoices/{invoiceId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await ParseAsync<InvoiceDto>(response);

        dto.Id.ShouldBe(invoiceId);
        dto.TenantId.ShouldBe(TestConstants.RootTenantId);
        dto.PeriodYear.ShouldBe(year);
        dto.PeriodMonth.ShouldBe(month);
        dto.Status.ShouldBe(InvoiceStatus.Draft);
        dto.LineItems.Count.ShouldBe(1, "SeedDraftInvoiceAsync always adds a single BaseFee line");
        dto.SubtotalAmount.ShouldBe(19.99m);
    }

    [Fact]
    public async Task GetInvoiceById_Should_Return404_For_UnknownId()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync($"{BillingBasePath}/invoices/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvoices_Should_Filter_By_Period()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync(
            $"{BillingBasePath}/invoices?periodYear={year}&periodMonth={month}&pageNumber=1&pageSize=50");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await ParseAsync<PagedResponse<InvoiceDto>>(response);

        page.Items.ShouldContain(i => i.Id == invoiceId);
        page.Items.ShouldAllBe(i => i.PeriodYear == year && i.PeriodMonth == month);
    }

    [Fact]
    public async Task GetInvoices_Should_Filter_By_Status()
    {
        // Seed two draft invoices in unrelated periods so the status filter is the only thing
        // narrowing the result — guards against accidentally widening the query later.
        var (year1, month1) = NextPeriod();
        var (year2, month2) = NextPeriod();
        var draftId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year1, month1);
        var paidSourceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year2, month2);

        // Push the second one through Issue then Paid so the two seeded rows sit in different
        // statuses; we can then verify ?status=Paid returns the paid one and not the draft.
        await SeedDirectAsync(async db =>
        {
            var inv = await db.Invoices.FindAsync(paidSourceId);
            inv.ShouldNotBeNull();
            inv!.Issue(dueAtUtc: null);
            inv.MarkPaid();
            await db.SaveChangesAsync();
        });
        using var client = await _auth.CreateRootAdminClientAsync();

        using var paidResponse = await client.GetAsync(
            $"{BillingBasePath}/invoices?status=Paid&pageNumber=1&pageSize=200");
        var paidPage = await ParseAsync<PagedResponse<InvoiceDto>>(paidResponse);

        paidPage.Items.ShouldContain(i => i.Id == paidSourceId);
        paidPage.Items.ShouldNotContain(i => i.Id == draftId);
        paidPage.Items.ShouldAllBe(i => i.Status == InvoiceStatus.Paid);
    }

    [Fact]
    public async Task GetMyInvoices_Should_Return_Current_Tenant_Invoices_Only()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync(
            $"{BillingBasePath}/invoices/me?periodYear={year}&periodMonth={month}&pageNumber=1&pageSize=50");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await ParseAsync<PagedResponse<InvoiceDto>>(response);

        page.Items.ShouldContain(i => i.Id == invoiceId);
        page.Items.ShouldAllBe(i => i.TenantId == TestConstants.RootTenantId);
    }

    // ─── Invoices: lifecycle ─────────────────────────────────────────

    [Fact]
    public async Task IssueInvoice_Should_Transition_Draft_To_Issued_With_Default_DueDate()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var issueResp = await client.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{invoiceId}/issue",
            new { dueAtUtc = (DateTime?)null });
        issueResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await GetInvoiceAsync(client, invoiceId);
        dto.Status.ShouldBe(InvoiceStatus.Issued);
        dto.IssuedAtUtc.ShouldNotBeNull();
        dto.DueAtUtc.ShouldNotBeNull();
        var defaultDueDelta = (dto.DueAtUtc!.Value - dto.IssuedAtUtc!.Value).TotalDays;
        defaultDueDelta.ShouldBe(14.0, tolerance: 0.01, "Default due-date is +14 days from issued time");
    }

    [Fact]
    public async Task IssueInvoice_Should_Use_Custom_DueDate_When_Provided()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        var customDue = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        using var issueResp = await client.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{invoiceId}/issue",
            new { dueAtUtc = customDue });
        issueResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await GetInvoiceAsync(client, invoiceId);
        dto.Status.ShouldBe(InvoiceStatus.Issued);
        dto.DueAtUtc.ShouldNotBeNull();
        dto.DueAtUtc!.Value.Date.ShouldBe(customDue.Date);
    }

    [Fact]
    public async Task IssueInvoice_Should_Reject_NonDraft_Invoice()
    {
        // The Invoice aggregate's RequireStatus(Draft) guard surfaces as InvalidOperationException
        // which the global handler converts to 5xx. We don't pin the exact status — just that the
        // call did not succeed AND the invoice state is unchanged.
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month);
        await SeedDirectAsync(async db =>
        {
            var inv = await db.Invoices.FindAsync(invoiceId);
            inv!.Issue(dueAtUtc: null);
            await db.SaveChangesAsync();
        });
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{invoiceId}/issue",
            new { dueAtUtc = (DateTime?)null });

        response.IsSuccessStatusCode.ShouldBeFalse(
            "Issuing an already-Issued invoice must fail at the domain guard");

        var dto = await GetInvoiceAsync(client, invoiceId);
        dto.Status.ShouldBe(InvoiceStatus.Issued, "state must not regress on a failed re-issue");
    }

    [Fact]
    public async Task MarkInvoicePaid_Should_Transition_Issued_To_Paid()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedIssuedInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsync($"{BillingBasePath}/invoices/{invoiceId}/pay", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await GetInvoiceAsync(client, invoiceId);
        dto.Status.ShouldBe(InvoiceStatus.Paid);
        dto.PaidAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkInvoicePaid_Should_Reject_Draft_Invoice()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsync($"{BillingBasePath}/invoices/{invoiceId}/pay", content: null);

        response.IsSuccessStatusCode.ShouldBeFalse(
            "Paying a Draft invoice must fail — only Issued invoices can transition to Paid");
        (await GetInvoiceAsync(client, invoiceId)).Status.ShouldBe(InvoiceStatus.Draft);
    }

    [Fact]
    public async Task MarkInvoicePaid_Should_Be_Idempotent_On_AlreadyPaid()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedIssuedInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var first = await client.PostAsync($"{BillingBasePath}/invoices/{invoiceId}/pay", content: null);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstStamp = (await GetInvoiceAsync(client, invoiceId)).PaidAtUtc;
        firstStamp.ShouldNotBeNull();

        // Aggregate uses `if (Status is Paid) return;` — second call must succeed and not mutate.
        await Task.Delay(20);
        using var second = await client.PostAsync($"{BillingBasePath}/invoices/{invoiceId}/pay", content: null);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await GetInvoiceAsync(client, invoiceId);
        dto.Status.ShouldBe(InvoiceStatus.Paid);
        dto.PaidAtUtc.ShouldBe(firstStamp, "Second MarkPaid call must not re-stamp PaidAtUtc");
    }

    [Fact]
    public async Task VoidInvoice_Should_Transition_Draft_To_Void()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{invoiceId}/void",
            new { reason = "duplicate" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await GetInvoiceAsync(client, invoiceId);
        dto.Status.ShouldBe(InvoiceStatus.Void);
        dto.VoidedAtUtc.ShouldNotBeNull();
        dto.Notes.ShouldNotBeNullOrWhiteSpace();
        dto.Notes!.ShouldContain("duplicate");
    }

    [Fact]
    public async Task VoidInvoice_Should_Transition_Issued_To_Void()
    {
        var (year, month) = NextPeriod();
        var invoiceId = await SeedIssuedInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{invoiceId}/void", new { reason = (string?)null });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await GetInvoiceAsync(client, invoiceId)).Status.ShouldBe(InvoiceStatus.Void);
    }

    [Fact]
    public async Task VoidInvoice_Should_Reject_Paid_Invoice()
    {
        // The aggregate explicitly throws "Paid invoices cannot be voided." — the test pins the
        // refusal so a future relaxation of that rule has to be explicit.
        var (year, month) = NextPeriod();
        var invoiceId = await SeedIssuedInvoiceAsync(TestConstants.RootTenantId, year, month);
        using var payClient = await _auth.CreateRootAdminClientAsync();
        using var pay = await payClient.PostAsync($"{BillingBasePath}/invoices/{invoiceId}/pay", content: null);
        pay.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var voidResp = await payClient.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{invoiceId}/void", new { reason = "shouldn't work" });

        voidResp.IsSuccessStatusCode.ShouldBeFalse(
            "Paid invoices must not be voidable");
        (await GetInvoiceAsync(payClient, invoiceId)).Status.ShouldBe(InvoiceStatus.Paid,
            "state must not regress when Void is rejected");
    }

    [Fact]
    public async Task IssueInvoice_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/{Guid.NewGuid()}/issue", new { dueAtUtc = (DateTime?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── Invoices: generation (end-to-end through Plan + Subscription) ─

    [Fact]
    public async Task GenerateInvoices_Should_Generate_For_Subscribed_Tenants_And_Be_Idempotent()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("gen");
        await CreatePlanAsync(client, key, name: "Plan-gen", monthlyBasePrice: 7m);
        await AssignSubscriptionAsync(client, TestConstants.RootTenantId, key);

        var (year, month) = NextPeriod();

        using var firstResp = await client.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/generate", new { periodYear = year, periodMonth = month });
        firstResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstPayload = await ParseAsync<GeneratedPayload>(firstResp);
        firstPayload.Generated.ShouldBeGreaterThanOrEqualTo(1,
            "Root tenant has an active subscription so at least one invoice should generate");

        // Verify the invoice exists for root in this period.
        using var listResp = await client.GetAsync(
            $"{BillingBasePath}/invoices?tenantId={TestConstants.RootTenantId}&periodYear={year}&periodMonth={month}&pageNumber=1&pageSize=10");
        var page = await ParseAsync<PagedResponse<InvoiceDto>>(listResp);
        page.Items.Count.ShouldBe(1);
        var inv = page.Items.Single();
        inv.Status.ShouldBe(InvoiceStatus.Draft);
        inv.SubtotalAmount.ShouldBeGreaterThanOrEqualTo(7m, "base fee from the assigned plan should be in the subtotal");

        // Second call must be idempotent — no new invoices for the same period.
        using var secondResp = await client.PostAsJsonAsync(
            $"{BillingBasePath}/invoices/generate", new { periodYear = year, periodMonth = month });
        secondResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondPayload = await ParseAsync<GeneratedPayload>(secondResp);
        secondPayload.Generated.ShouldBe(0, "Re-running generate for the same period must skip already-invoiced tenants");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private sealed record GeneratedPayload(int Generated);

    /// <summary>Plan keys are lowercased + unique-indexed; this helper guarantees no collisions.</summary>
    private static string UniqueKey(string prefix) =>
        $"plan-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>
    /// Returns a (Year, Month) that no other test in this class has used yet. Years start at 2080
    /// to keep the test data well away from any seed/production-shaped data and any year filter the
    /// app might use elsewhere.
    /// </summary>
    private static (int Year, int Month) NextPeriod()
    {
        int n = Interlocked.Increment(ref s_periodCounter);
        int yearOffset = (n - 1) / 12;
        int month = ((n - 1) % 12) + 1;
        return (2080 + yearOffset, month);
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

    private static async Task<IReadOnlyList<BillingPlanDto>> GetPlansAsync(HttpClient client, bool includeInactive = false)
    {
        using var response = await client.GetAsync(
            $"{BillingBasePath}/plans?includeInactive={(includeInactive ? "true" : "false")}");
        return await ParseAsync<IReadOnlyList<BillingPlanDto>>(response);
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

    private static async Task<InvoiceDto> GetInvoiceAsync(HttpClient client, Guid invoiceId)
    {
        using var response = await client.GetAsync($"{BillingBasePath}/invoices/{invoiceId}");
        return await ParseAsync<InvoiceDto>(response);
    }

    private async Task<Guid> SeedDraftInvoiceAsync(string tenantId, int year, int month, decimal basePrice = 10m)
    {
        Guid id = Guid.Empty;
        await SeedDirectAsync(async db =>
        {
            var invoiceNumber = $"INV-TEST-{year}{month:00}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            var inv = Invoice.CreateDraft(tenantId, invoiceNumber, year, month, "USD");
            inv.AddLineItem(InvoiceLineItemKind.BaseFee, "Seeded base fee", 1m, basePrice);
            db.Invoices.Add(inv);
            await db.SaveChangesAsync();
            id = inv.Id;
        });
        return id;
    }

    private async Task<Guid> SeedIssuedInvoiceAsync(string tenantId, int year, int month)
    {
        var id = await SeedDraftInvoiceAsync(tenantId, year, month);
        await SeedDirectAsync(async db =>
        {
            var inv = await db.Invoices.FindAsync(id);
            inv.ShouldNotBeNull();
            inv!.Issue(dueAtUtc: null);
            await db.SaveChangesAsync();
        });
        return id;
    }

    private async Task SeedDirectAsync(Func<BillingDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await action(db);
    }
}
