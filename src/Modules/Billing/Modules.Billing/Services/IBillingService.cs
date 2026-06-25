using FSH.Modules.Billing.Domain;

namespace FSH.Modules.Billing.Services;

/// <summary>
/// Core billing workflow: snapshot usage, price it, issue the invoice, track payment state.
/// Payment processor integration is intentionally out of scope — invoices are marked paid manually.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Returns the wallet for <paramref name="tenantId"/>, creating one if none exists.
    /// The wallet is the single balance ledger per tenant for prepaid credit (e.g. WhatsApp).
    /// </summary>
    Task<Wallet> GetOrCreateWalletAsync(string tenantId, string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and issues a Topup-purpose invoice for the pending <see cref="TopupRequest"/>,
    /// fires <c>InvoiceIssuedIntegrationEvent</c>, calls <c>request.MarkInvoiced</c>, and saves —
    /// all in one unit of work.
    /// </summary>
    Task<Invoice> CreateTopupInvoiceAsync(string tenantId, Guid topupRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a Draft invoice for the tenant/period by snapshotting usage and pricing it against
    /// the tenant's active subscription plan. Returns null if the tenant has no active subscription
    /// or an invoice already exists for the period.
    /// </summary>
    Task<Invoice?> GenerateInvoiceForPeriodAsync(
        string tenantId,
        int periodYear,
        int periodMonth,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates draft invoices for every tenant with an active subscription for the given period.
    /// Returns the count of invoices created (existing invoices for the period are skipped).
    /// </summary>
    Task<int> GenerateInvoicesForAllTenantsAsync(
        int periodYear,
        int periodMonth,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and issues a Subscription-purpose invoice for one plan term (the term base fee). Called
    /// when a tenant subscribes or renews. Returns null for free/zero-price plans (no invoice).
    /// Idempotent: returns the existing invoice if one already exists for the term.
    /// </summary>
    Task<Invoice?> CreateSubscriptionInvoiceAsync(
        string tenantId,
        Guid planId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default);

    Task IssueInvoiceAsync(Guid invoiceId, DateTime? dueAtUtc, CancellationToken cancellationToken = default);

    Task MarkInvoicePaidAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task VoidInvoiceAsync(Guid invoiceId, string? reason, CancellationToken cancellationToken = default);
}
