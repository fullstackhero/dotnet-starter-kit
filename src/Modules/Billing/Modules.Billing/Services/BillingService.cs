using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.Events;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Billing.Services;

public sealed class BillingService : IBillingService
{
    private readonly BillingDbContext _db;
    private readonly IUsageReporter _usageReporter;
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly IEventBus _eventBus;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        BillingDbContext db,
        IUsageReporter usageReporter,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        IEventBus eventBus,
        TimeProvider timeProvider,
        ILogger<BillingService> logger)
    {
        _db = db;
        _usageReporter = usageReporter;
        _tenantStore = tenantStore;
        _tenantAccessor = tenantAccessor;
        _eventBus = eventBus;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Invoice?> GenerateInvoiceForPeriodAsync(
        string tenantId,
        int periodYear,
        int periodMonth,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // Scope the idempotency check to Purpose==Usage: a Subscription invoice may legitimately share
        // the month, and without this filter we'd match it and skip the usage/overage invoice (unbilled overage).
        var existing = await _db.Invoices
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.PeriodYear == periodYear && i.PeriodMonth == periodMonth
                && i.Purpose == InvoicePurpose.Usage, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[Billing] usage invoice already exists for tenant {TenantId} period {Year}-{Month:00}, skipping",
                    tenantId, periodYear, periodMonth);
            }
            return existing;
        }

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            _logger.LogWarning("[Billing] no active subscription for tenant {TenantId}, skipping invoice", tenantId);
            return null;
        }

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Plan {subscription.PlanId} not found for tenant {tenantId}.");

        var snapshots = await _usageReporter.CaptureForPeriodAsync(tenantId, periodYear, periodMonth, cancellationToken).ConfigureAwait(false);

        // Usage invoices bill metered overage only. The plan's base fee is billed by the
        // subscription invoice on tenant create/renew (see CreateSubscriptionInvoiceAsync), so it is
        // intentionally NOT added here — otherwise monthly plans would be double-billed.
        var invoiceNumber = BuildUsageInvoiceNumber(tenantId, periodYear, periodMonth);
        var invoice = Invoice.CreateDraft(tenantId, invoiceNumber, periodYear, periodMonth, plan.Currency,
            InvoicePurpose.Usage, periodStartUtc: null, periodEndUtc: null);

        foreach (var snap in snapshots)
        {
            if (snap.Overage <= 0)
            {
                continue;
            }
            var rate = plan.GetOverageRate(snap.Resource);
            if (rate <= 0)
            {
                continue;
            }
            var line = invoice.AddLineItem(
                InvoiceLineItemKind.Overage,
                $"{snap.Resource} overage ({snap.Overage} units)",
                snap.Overage,
                rate);
            line.AttachResource(snap.Resource);
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Billing] generated draft invoice {InvoiceNumber} for tenant {TenantId} period {Year}-{Month:00} total={Total} {Currency}",
                invoice.InvoiceNumber, tenantId, periodYear, periodMonth, invoice.SubtotalAmount, invoice.Currency);
        }
        return invoice;
    }

    public async Task<int> GenerateInvoicesForAllTenantsAsync(
        int periodYear,
        int periodMonth,
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantStore.GetAllAsync().ConfigureAwait(false);
        var activeTenantIds = tenants.Where(t => t.IsActive).Select(t => t.Id).ToList();
        var subscribedTenantIds = await _db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && activeTenantIds.Contains(s.TenantId))
            .Select(s => s.TenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var alreadyInvoiced = await _db.Invoices
            .Where(i => i.PeriodYear == periodYear && i.PeriodMonth == periodMonth
                && i.Purpose == InvoicePurpose.Usage && subscribedTenantIds.Contains(i.TenantId))
            .Select(i => i.TenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var toGenerate = subscribedTenantIds.Except(alreadyInvoiced, StringComparer.Ordinal).ToList();

        var generated = 0;
        foreach (var tenantId in toGenerate)
        {
            try
            {
                var inv = await GenerateInvoiceForPeriodAsync(tenantId, periodYear, periodMonth, cancellationToken).ConfigureAwait(false);
                if (inv is not null)
                {
                    generated++;
                }
            }
#pragma warning disable CA1031 // One tenant's failure must not block the others
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "[Billing] failed generating invoice for tenant {TenantId} period {Year}-{Month:00}",
                    tenantId, periodYear, periodMonth);
            }
        }
        return generated;
    }

    public async Task IssueInvoiceAsync(Guid invoiceId, DateTime? dueAtUtc, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        invoice.Issue(dueAtUtc);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Wallet> GetOrCreateWalletAsync(string tenantId, string currency, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (wallet is null)
        {
            wallet = Wallet.Create(tenantId, currency);
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return wallet;
    }

    public async Task<Invoice> CreateTopupInvoiceAsync(string tenantId, Guid topupRequestId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var request = await _db.TopupRequests
            .FirstOrDefaultAsync(r => r.Id == topupRequestId && r.TenantId == tenantId && r.Status == TopupRequestStatus.Pending, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Top-up request {topupRequestId} not found or not pending.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var invoiceNumber = BuildTopupInvoiceNumber(tenantId, now, topupRequestId);

        var invoice = Invoice.CreateTopupDraft(
            tenantId,
            invoiceNumber,
            now.Year,
            now.Month,
            request.Currency,
            request.Amount,
            $"WhatsApp wallet top-up ({request.Amount:0.##} {request.Currency})");

        invoice.Issue();
        _db.Invoices.Add(invoice);
        request.MarkInvoiced(invoice.Id, request.Note);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[Billing] issued top-up invoice {InvoiceNumber} for tenant {TenantId} amount={Amount} {Currency}",
                invoice.InvoiceNumber, tenantId, invoice.SubtotalAmount, invoice.Currency);
        }

        await _eventBus.PublishAsync(new InvoiceIssuedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: now,
            TenantId: tenantId,
            CorrelationId: Guid.NewGuid().ToString(),
            Source: "Billing",
            InvoiceId: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            Amount: invoice.SubtotalAmount,
            Currency: invoice.Currency,
            DueAtUtc: invoice.DueAtUtc,
            PeriodYear: invoice.PeriodYear,
            PeriodMonth: invoice.PeriodMonth), cancellationToken).ConfigureAwait(false);

        return invoice;
    }

    public async Task MarkInvoicePaidAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        invoice.MarkPaid();

        // When a top-up invoice is paid, credit the tenant's wallet and complete the request —
        // all in the same SaveChanges so the credit + status flip are atomic.
        if (invoice.Purpose == InvoicePurpose.Topup)
        {
            var topupRequest = await _db.TopupRequests
                .FirstOrDefaultAsync(r => r.InvoiceId == invoice.Id, cancellationToken)
                .ConfigureAwait(false);

            if (topupRequest is { Status: TopupRequestStatus.Invoiced })
            {
                var wallet = await _db.Wallets
                    .FirstOrDefaultAsync(w => w.TenantId == invoice.TenantId, cancellationToken)
                    .ConfigureAwait(false);

                if (wallet is null)
                {
                    wallet = Wallet.Create(invoice.TenantId, invoice.Currency);
                    _db.Wallets.Add(wallet);
                }

                wallet.Credit(
                    invoice.SubtotalAmount,
                    WalletTransactionKind.Topup,
                    "WhatsApp wallet top-up",
                    topupRequest.Id.ToString());

                topupRequest.MarkCompleted();
            }
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task VoidInvoiceAsync(Guid invoiceId, string? reason, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        invoice.Void(reason);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // Issue/MarkPaid/Void load here. BillingDbContext isn't tenant-filtered, so scope to caller: root
    // mutates any invoice; a tenant caller is pinned to its own (cross-tenant id → 404, can't mutate).
    private async Task<Invoice> LoadInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var callerTenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.");
        var isRoot = callerTenantId == MultitenancyConstants.Root.Id;

        return await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && (isRoot || i.TenantId == callerTenantId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found.");
    }

    public async Task<Invoice?> CreateSubscriptionInvoiceAsync(
        string tenantId,
        Guid planId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Plan {planId} not found for tenant {tenantId}.");

        var termPrice = plan.TermPrice;
        if (termPrice <= 0)
        {
            // Free / trial plan — validity is still set, but there is nothing to bill.
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[Billing] plan {PlanKey} term price is zero for tenant {TenantId}, no subscription invoice", plan.Key, tenantId);
            }
            return null;
        }

        var periodStart = DateTime.SpecifyKind(periodStartUtc, DateTimeKind.Utc);
        var periodEnd = DateTime.SpecifyKind(periodEndUtc, DateTimeKind.Utc);
        var invoiceNumber = BuildSubscriptionInvoiceNumber(tenantId, periodStart);

        // Idempotency: redelivery of the subscribe/renew event must not double-invoice the term.
        var existing = await _db.Invoices
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.InvoiceNumber == invoiceNumber, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var invoice = Invoice.CreateDraft(tenantId, invoiceNumber, periodStart.Year, periodStart.Month,
            plan.Currency, InvoicePurpose.Subscription, periodStart, periodEnd);
        invoice.AddLineItem(
            InvoiceLineItemKind.BaseFee,
            $"{plan.Name} — {plan.Interval} subscription ({periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd})",
            1m,
            termPrice);
        invoice.Issue();

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Billing] issued subscription invoice {InvoiceNumber} for tenant {TenantId} total={Total} {Currency}",
                invoice.InvoiceNumber, tenantId, invoice.SubtotalAmount, invoice.Currency);
        }

        // Notify (e.g. email the tenant) that a real bill was issued. Only fires for newly-created
        // invoices — the idempotent early-return above skips this on event redelivery.
        await _eventBus.PublishAsync(new InvoiceIssuedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: _timeProvider.GetUtcNow().UtcDateTime,
            TenantId: tenantId,
            CorrelationId: Guid.NewGuid().ToString(),
            Source: "Billing",
            InvoiceId: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            Amount: invoice.SubtotalAmount,
            Currency: invoice.Currency,
            DueAtUtc: invoice.DueAtUtc,
            PeriodYear: invoice.PeriodYear,
            PeriodMonth: invoice.PeriodMonth), cancellationToken).ConfigureAwait(false);

        return invoice;
    }

    private static string BuildUsageInvoiceNumber(string tenantId, int periodYear, int periodMonth) =>
        $"USG-{periodYear}{periodMonth:00}-{TenantToken(tenantId)}";

    private static string BuildSubscriptionInvoiceNumber(string tenantId, DateTime periodStartUtc) =>
        $"SUB-{periodStartUtc:yyyyMM}-{TenantToken(tenantId)}";

    /// <summary>
    /// Generates a collision-safe invoice number for a top-up.
    /// Format: <c>TOP-{yyyyMM}-{tenantToken}-{requestSuffix}</c>
    /// where <c>requestSuffix</c> is 8 hex chars from the last 4 bytes of <paramref name="topupRequestId"/>.
    /// Each <see cref="TopupRequest"/> has a unique <see cref="Guid"/>, so two top-ups for the same
    /// tenant in the same month produce distinct numbers and never collide on the unique InvoiceNumber index.
    /// </summary>
    private static string BuildTopupInvoiceNumber(string tenantId, DateTime now, Guid topupRequestId)
    {
        var suffix = Convert.ToHexString(topupRequestId.ToByteArray(), 12, 4);
        return $"TOP-{now:yyyyMM}-{TenantToken(tenantId)}-{suffix}";
    }

    // Stable, collision-resistant token from the full tenant id; a naive prefix truncation would
    // collide for shared-prefix tenants and clash on the unique InvoiceNumber index.
    private static string TenantToken(string tenantId)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tenantId));
        return Convert.ToHexString(hash, 0, 6);
    }
}
