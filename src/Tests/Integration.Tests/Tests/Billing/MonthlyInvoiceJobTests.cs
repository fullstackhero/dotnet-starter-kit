using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using FSH.Modules.Billing.Services;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// Drives <see cref="MonthlyInvoiceJob"/> directly. UsageMeteringTests only asserts the recurring job
/// is *scheduled* in Hangfire; the job body (RunAsync → BillingService.GenerateInvoicesForAllTenantsAsync)
/// is never executed because Hangfire jobs do not fire in the test host. Here we activate the job from a
/// DI scope (it is not registered itself, but its dependencies IBillingService + ILogger are) and invoke
/// RunAsync, having first given the root tenant an active subscription for the period the job bills
/// (UtcNow - 1 month). We then assert a draft invoice was generated for that period and that a second
/// run is idempotent.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class MonthlyInvoiceJobTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public MonthlyInvoiceJobTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task RunAsync_Should_Generate_Draft_Invoice_For_Subscribed_Tenant_For_Previous_Period()
    {
        // Arrange — the job bills the calendar month immediately before now.
        var previous = DateTime.UtcNow.AddMonths(-1);
        await EnsureRootHasActiveSubscriptionAsync(monthlyBasePrice: 12.34m);
        await ClearRootInvoiceForPeriodAsync(previous.Year, previous.Month);

        // Act
        await RunJobAsync();

        // Assert — root now has exactly the one draft invoice the job produced for the previous period.
        var rootCount = await CountRootInvoicesForPeriodAsync(previous.Year, previous.Month);
        rootCount.ShouldBe(1,
            "root has an active subscription so the previous-period run must generate exactly one invoice");

        var invoice = await GetRootInvoiceForPeriodAsync(previous.Year, previous.Month);
        invoice.ShouldNotBeNull("MonthlyInvoiceJob must produce a draft invoice for the subscribed root tenant");
        invoice!.Status.ShouldBe(InvoiceStatus.Draft);
        invoice.PeriodYear.ShouldBe(previous.Year);
        invoice.PeriodMonth.ShouldBe(previous.Month);
        invoice.SubtotalAmount.ShouldBeGreaterThanOrEqualTo(12.34m, "the assigned plan's base fee must land on the invoice");
    }

    #endregion

    #region Idempotency

    [Fact]
    public async Task RunAsync_Should_Be_Idempotent_Across_Repeated_Runs_For_Same_Period()
    {
        // Arrange
        var previous = DateTime.UtcNow.AddMonths(-1);
        await EnsureRootHasActiveSubscriptionAsync(monthlyBasePrice: 5m);
        await ClearRootInvoiceForPeriodAsync(previous.Year, previous.Month);

        // Act — first run generates, second run must skip the already-invoiced tenant.
        await RunJobAsync();
        var rootCountAfterFirst = await CountRootInvoicesForPeriodAsync(previous.Year, previous.Month);
        await RunJobAsync();
        var rootCountAfterSecond = await CountRootInvoicesForPeriodAsync(previous.Year, previous.Month);

        // Assert
        rootCountAfterFirst.ShouldBe(1, "first run must create exactly one invoice for root");
        rootCountAfterSecond.ShouldBe(1, "re-running the job must not duplicate the root invoice for the same period");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Activates the (non-DI-registered) job through ActivatorUtilities so its scoped IBillingService is
    /// constructed inside the same scope, sets the Finbuckle tenant context inline (the store/billing
    /// queries NRE on a null MultiTenantContext otherwise), and invokes RunAsync — the real production
    /// entrypoint. RunAsync returns void, so callers assert via the period-scoped invoice queries below.
    /// </summary>
    private async Task RunJobAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var job = ActivatorUtilities.CreateInstance<MonthlyInvoiceJob>(scope.ServiceProvider);
        await job.RunAsync(CancellationToken.None);
    }

    private async Task EnsureRootHasActiveSubscriptionAsync(decimal monthlyBasePrice)
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var key = $"plan-job-{Guid.NewGuid().ToString("N")[..8]}";

        using var planResp = await client.PostAsJsonAsync($"{BillingBasePath}/plans",
            new { key, name = "Plan-job", currency = "USD", monthlyBasePrice });
        planResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var subResp = await client.PostAsJsonAsync($"{BillingBasePath}/subscriptions",
            new { tenantId = TestConstants.RootTenantId, planKey = key });
        subResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task ClearRootInvoiceForPeriodAsync(int year, int month)
    {
        await SeedDirectAsync(async db =>
        {
            var existing = await db.Invoices
                .Where(i => i.TenantId == TestConstants.RootTenantId && i.PeriodYear == year && i.PeriodMonth == month)
                .ToListAsync();
            if (existing.Count > 0)
            {
                db.Invoices.RemoveRange(existing);
                await db.SaveChangesAsync();
            }
        });
    }

    private async Task<int> CountRootInvoicesForPeriodAsync(int year, int month)
    {
        int count = 0;
        await SeedDirectAsync(async db =>
        {
            count = await db.Invoices
                .CountAsync(i => i.TenantId == TestConstants.RootTenantId && i.PeriodYear == year && i.PeriodMonth == month);
        });
        return count;
    }

    private async Task<Invoice?> GetRootInvoiceForPeriodAsync(int year, int month)
    {
        Invoice? invoice = null;
        await SeedDirectAsync(async db =>
        {
            invoice = await db.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.TenantId == TestConstants.RootTenantId && i.PeriodYear == year && i.PeriodMonth == month);
        });
        return invoice;
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
