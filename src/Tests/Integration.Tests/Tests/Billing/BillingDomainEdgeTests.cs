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
/// Targets the remaining uncovered domain branches that the endpoint-flow tests in BillingEndpointTests
/// don't reach: BillingPlan overage-rate create/update/lookup, Subscription Suspend/Reactivate/Cancel
/// state transitions (no public endpoint drives these), and InvoiceLineItem overage lines / resource
/// attach / amount rounding. Overage rates flow through the public Create/Update Plan endpoints; the
/// subscription and line-item edges are exercised through the domain aggregates inside a tenant-scoped
/// DbContext and verified by reading the persisted state back.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class BillingDomainEdgeTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static int s_periodCounter;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public BillingDomainEdgeTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region BillingPlan — overage rates

    [Fact]
    public async Task CreatePlan_With_OverageRates_Should_Persist_And_Expose_Them()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("ovr-create");

        // Act — overage-rate dictionary keyed by QuotaResource (serialized as enum names).
        using var response = await client.PostAsJsonAsync($"{BillingBasePath}/plans", new
        {
            key,
            name = "Plan-overage",
            currency = "USD",
            monthlyBasePrice = 10m,
            overageRates = new Dictionary<string, decimal>
            {
                [nameof(QuotaResource.ApiCalls)] = 0.01m,
                [nameof(QuotaResource.StorageBytes)] = 0.50m,
            }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var planId = await response.DeserializeAsync<Guid>();

        var plan = await GetPlanAsync(client, planId);
        plan.OverageRates[QuotaResource.ApiCalls].ShouldBe(0.01m);
        plan.OverageRates[QuotaResource.StorageBytes].ShouldBe(0.50m);

        // Domain lookup: a configured resource returns its rate, an unconfigured one returns 0.
        await SeedDirectAsync(async db =>
        {
            var domain = await db.Plans.FindAsync(planId);
            domain.ShouldNotBeNull();
            domain!.GetOverageRate(QuotaResource.ApiCalls).ShouldBe(0.01m);
            domain.GetOverageRate(QuotaResource.Users).ShouldBe(0m,
                "GetOverageRate returns 0 for a resource with no configured rate");
        });
    }

    [Fact]
    public async Task UpdatePlan_Should_Replace_OverageRates_Wholesale()
    {
        // BillingPlan.Update clears the existing rate map then repopulates from the supplied dictionary.
        // Arrange — start with one rate set...
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = UniqueKey("ovr-update");
        using var createResp = await client.PostAsJsonAsync($"{BillingBasePath}/plans", new
        {
            key,
            name = "Before",
            currency = "USD",
            monthlyBasePrice = 5m,
            overageRates = new Dictionary<string, decimal> { [nameof(QuotaResource.ApiCalls)] = 0.05m },
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var planId = await createResp.DeserializeAsync<Guid>();

        // Act — update with a completely different rate map.
        using var updateResp = await client.PutAsJsonAsync($"{BillingBasePath}/plans/{planId}", new
        {
            planId,
            name = "After",
            monthlyBasePrice = 9m,
            overageRates = new Dictionary<string, decimal> { [nameof(QuotaResource.Users)] = 2m },
        });
        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert — old rate is gone, new rate is present (clear-then-repopulate, not merge).
        var plan = await GetPlanAsync(client, planId);
        plan.Name.ShouldBe("After");
        plan.MonthlyBasePrice.ShouldBe(9m);
        plan.OverageRates.ContainsKey(QuotaResource.ApiCalls).ShouldBeFalse(
            "Update must clear the prior overage rates rather than merge them");
        plan.OverageRates[QuotaResource.Users].ShouldBe(2m);
    }

    #endregion

    #region Subscription — state transitions

    [Fact]
    public async Task Subscription_Suspend_Then_Reactivate_Should_Round_Trip_Status()
    {
        // No endpoint drives Suspend/Reactivate; exercise the aggregate directly and read state back.
        // Arrange
        var subId = await SeedActiveSubscriptionAsync();

        // Act — Suspend
        await SeedDirectAsync(async db =>
        {
            var sub = await db.Subscriptions.FindAsync(subId);
            sub!.Suspend();
            await db.SaveChangesAsync();
        });

        // Assert
        await AssertSubscriptionStatusAsync(subId, SubscriptionStatus.Suspended);

        // Act — Reactivate
        await SeedDirectAsync(async db =>
        {
            var sub = await db.Subscriptions.FindAsync(subId);
            sub!.Reactivate();
            await db.SaveChangesAsync();
        });

        // Assert
        await AssertSubscriptionStatusAsync(subId, SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Subscription_Cancel_Should_Set_Status_And_EndUtc()
    {
        // Arrange
        var subId = await SeedActiveSubscriptionAsync();
        var endUtc = new DateTime(2090, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        await SeedDirectAsync(async db =>
        {
            var sub = await db.Subscriptions.FindAsync(subId);
            sub!.Cancel(endUtc);
            await db.SaveChangesAsync();
        });

        // Assert
        await SeedDirectAsync(async db =>
        {
            var sub = await db.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subId);
            sub.Status.ShouldBe(SubscriptionStatus.Cancelled);
            sub.EndUtc.ShouldNotBeNull();
            sub.EndUtc!.Value.Date.ShouldBe(endUtc.Date);
            sub.EndUtc.Value.Kind.ShouldBe(DateTimeKind.Utc, "Cancel must normalize EndUtc to UTC kind");
        });
    }

    #endregion

    #region InvoiceLineItem — overage line, amount rounding, subtotal recompute

    [Fact]
    public async Task AddLineItem_Overage_Should_Round_Amount_AwayFromZero_And_Recompute_Subtotal()
    {
        // Build the invoice fresh (BaseFee + Overage). Overage 2.5*0.01=0.025 must round AwayFromZero to
        // 0.03 (not banker's 0.02), and the subtotal must recompute across both lines.
        // Arrange / Act
        var (year, month) = NextPeriod();
        Guid invoiceId = Guid.Empty;
        await SeedDirectAsync(async db =>
        {
            var invoiceNumber = $"INV-EDGE-{year}{month:00}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            var inv = Invoice.CreateDraft(TestConstants.RootTenantId, invoiceNumber, year, month, "USD");
            inv.AddLineItem(InvoiceLineItemKind.BaseFee, "Base fee", 1m, 10m);
            inv.AddLineItem(InvoiceLineItemKind.Overage, "ApiCalls overage", quantity: 2.5m, unitPrice: 0.01m);
            db.Invoices.Add(inv);
            await db.SaveChangesAsync();
            invoiceId = inv.Id;
        });

        // Assert
        await SeedDirectAsync(async db =>
        {
            var inv = await db.Invoices.AsNoTracking().Include(i => i.LineItems).FirstAsync(i => i.Id == invoiceId);
            var overage = inv.LineItems.Single(l => l.Kind == InvoiceLineItemKind.Overage);
            overage.Amount.ShouldBe(0.03m, "2.5 * 0.01 = 0.025 must round AwayFromZero to 0.03");

            // Subtotal recomputes across all lines (BaseFee 10 + overage 0.03).
            inv.SubtotalAmount.ShouldBe(10.03m, "AddLineItem must recompute SubtotalAmount over every line");
        });
    }

    [Fact]
    public async Task AddLineItem_Should_Reject_Negative_Quantity()
    {
        // Arrange
        var (year, month) = NextPeriod();
        var invoiceId = await SeedDraftInvoiceAsync(TestConstants.RootTenantId, year, month);

        // Act / Assert — the InvoiceLineItem.Create guard throws on negative quantity.
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
        {
            await SeedDirectAsync(async db =>
            {
                var inv = await db.Invoices.Include(i => i.LineItems).FirstAsync(i => i.Id == invoiceId);
                inv.AddLineItem(InvoiceLineItemKind.Adjustment, "bad", quantity: -1m, unitPrice: 1m);
                await db.SaveChangesAsync();
            });
        });
    }

    #endregion

    #region Helpers

    private static string UniqueKey(string prefix) => $"plan-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static (int Year, int Month) NextPeriod()
    {
        int n = Interlocked.Increment(ref s_periodCounter);
        int yearOffset = (n - 1) / 12;
        int month = ((n - 1) % 12) + 1;
        // 2070-block: distinct from BillingEndpointTests (2080+), UsageMeteringTests (2030/2031), and
        // UsageSnapshotQueryTests (2060+).
        return (2070 + yearOffset, month);
    }

    private static async Task<BillingPlanDto> GetPlanAsync(HttpClient client, Guid planId)
    {
        using var response = await client.GetAsync($"{BillingBasePath}/plans?includeInactive=true");
        var plans = await response.DeserializeAsync<IReadOnlyList<BillingPlanDto>>();
        return plans.Single(p => p.Id == planId);
    }

    private async Task<Guid> SeedActiveSubscriptionAsync()
    {
        // Throwaway tenant id, NOT root: a partial unique index allows one Active subscription per tenant
        // and root already has one. Billing's DbContext isn't tenant-filtered, so any tenant id works here.
        var tenantId = $"sub-edge-{Guid.NewGuid().ToString("N")[..8]}";
        Guid id = Guid.Empty;
        await SeedDirectAsync(async db =>
        {
            // A plan is needed for the FK-free Subscription.PlanId; any plan id works for status tests.
            var plan = BillingPlan.Create(UniqueKey("sub-edge"), "Sub-edge", "USD", 1m);
            db.Plans.Add(plan);
            var sub = Subscription.Create(tenantId, plan.Id, DateTime.UtcNow);
            db.Subscriptions.Add(sub);
            await db.SaveChangesAsync();
            id = sub.Id;
        });
        return id;
    }

    private async Task AssertSubscriptionStatusAsync(Guid subId, SubscriptionStatus expected)
    {
        await SeedDirectAsync(async db =>
        {
            var sub = await db.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subId);
            sub.Status.ShouldBe(expected);
        });
    }

    private async Task<Guid> SeedDraftInvoiceAsync(string tenantId, int year, int month, decimal basePrice = 10m)
    {
        Guid id = Guid.Empty;
        await SeedDirectAsync(async db =>
        {
            var invoiceNumber = $"INV-EDGE-{year}{month:00}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            var inv = Invoice.CreateDraft(tenantId, invoiceNumber, year, month, "USD");
            inv.AddLineItem(InvoiceLineItemKind.BaseFee, "Seeded base fee", 1m, basePrice);
            db.Invoices.Add(inv);
            await db.SaveChangesAsync();
            id = inv.Id;
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

    #endregion
}
