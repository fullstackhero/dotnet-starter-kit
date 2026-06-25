using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using FSH.Modules.Billing.Services;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// Integration tests for the WhatsApp wallet top-up flow:
///   CreateTopupInvoiceAsync → issues a Topup-purpose invoice and marks the request as Invoiced.
///   MarkInvoicePaidAsync → credits the wallet and marks the request as Completed (same UoW).
///   GetOrCreateWalletAsync → idempotent wallet retrieval.
///   Two top-ups in the same month succeed without an invoice-number/unique-index collision.
///
/// Each test uses a unique synthetic tenant id so wallet state never bleeds between runs.
/// The Finbuckle context is always set INLINE (AsyncLocal; lost across awaited helpers).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WalletTopupServiceTests
{
    private readonly FshWebApplicationFactory _factory;

    public WalletTopupServiceTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Happy-path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Topup_credits_wallet_when_invoice_marked_paid()
    {
        var tenantId = UniqueTestTenantId();

        using var scope = _factory.Services.CreateScope();

        // Set Finbuckle context INLINE — AsyncLocal propagates downward but NOT back up after await,
        // so the setter must be called directly in this method before any service calls.
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        // Arrange: seed a pending top-up request directly.
        var request = TopupRequest.Create(tenantId, 50m, "USD", "need credit", "user-1");
        db.TopupRequests.Add(request);
        await db.SaveChangesAsync();

        // Act: create the top-up invoice.
        var invoice = await billing.CreateTopupInvoiceAsync(tenantId, request.Id);

        invoice.Purpose.ShouldBe(InvoicePurpose.Topup);
        invoice.Status.ShouldBe(InvoiceStatus.Issued);

        // Act: mark it paid — should credit the wallet in the same UoW.
        await billing.MarkInvoicePaidAsync(invoice.Id);

        // Assert: wallet balance equals the top-up amount.
        var wallet = await billing.GetOrCreateWalletAsync(tenantId, "USD");
        wallet.Balance.ShouldBe(50m);

        // Assert: request transitioned to Completed.
        var reloaded = await db.TopupRequests.FindAsync(request.Id);
        reloaded!.Status.ShouldBe(TopupRequestStatus.Completed);
    }

    // ── Invoice-number uniqueness ────────────────────────────────────────────

    [Fact]
    public async Task Two_topups_in_same_month_both_succeed_with_unique_invoice_numbers()
    {
        var tenantId = UniqueTestTenantId();

        using var scope = _factory.Services.CreateScope();

        // Set context inline (AsyncLocal does not propagate back after awaited helpers).
        var tenantStore2 = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant2 = await tenantStore2.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant2);

        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        // Arrange: two pending requests for the same synthetic tenant in the current month.
        var req1 = TopupRequest.Create(tenantId, 25m, "USD", "first", "user-1");
        var req2 = TopupRequest.Create(tenantId, 75m, "USD", "second", "user-1");
        db.TopupRequests.AddRange(req1, req2);
        await db.SaveChangesAsync();

        // Act: create both top-up invoices. Should not throw despite same month/tenant.
        var inv1 = await billing.CreateTopupInvoiceAsync(tenantId, req1.Id);
        var inv2 = await billing.CreateTopupInvoiceAsync(tenantId, req2.Id);

        // Assert: both invoices are issued with Topup purpose and distinct numbers.
        inv1.Status.ShouldBe(InvoiceStatus.Issued);
        inv2.Status.ShouldBe(InvoiceStatus.Issued);
        inv1.Purpose.ShouldBe(InvoicePurpose.Topup);
        inv2.Purpose.ShouldBe(InvoicePurpose.Topup);
        inv1.InvoiceNumber.ShouldNotBe(inv2.InvoiceNumber,
            "two top-ups in the same month must have distinct invoice numbers");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Unique per-test tenant id — keeps wallet state isolated between tests.
    /// Does not need to be a real tenant in the store; BillingDbContext is not tenant-filtered.
    /// </summary>
    private static string UniqueTestTenantId() =>
        $"wt-{Guid.NewGuid().ToString("N")[..12]}";
}
