using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Contracts;
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
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        BillingDbContext db,
        IUsageReporter usageReporter,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        ILogger<BillingService> logger)
    {
        _db = db;
        _usageReporter = usageReporter;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    public async Task<Invoice?> GenerateInvoiceForPeriodAsync(
        string tenantId,
        int periodYear,
        int periodMonth,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var existing = await _db.Invoices
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.PeriodYear == periodYear && i.PeriodMonth == periodMonth, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[Billing] invoice already exists for tenant {TenantId} period {Year}-{Month:00}, skipping",
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

        var invoiceNumber = BuildInvoiceNumber(tenantId, periodYear, periodMonth);
        var invoice = Invoice.CreateDraft(tenantId, invoiceNumber, periodYear, periodMonth, plan.Currency);

        if (plan.MonthlyBasePrice > 0)
        {
            invoice.AddLineItem(InvoiceLineItemKind.BaseFee, $"{plan.Name} — base fee ({periodYear}-{periodMonth:00})", 1m, plan.MonthlyBasePrice);
        }

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
            .Where(i => i.PeriodYear == periodYear && i.PeriodMonth == periodMonth && subscribedTenantIds.Contains(i.TenantId))
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

    public async Task MarkInvoicePaidAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        invoice.MarkPaid();
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task VoidInvoiceAsync(Guid invoiceId, string? reason, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        invoice.Void(reason);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Invoice> LoadInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found.");

    private static string BuildInvoiceNumber(string tenantId, int periodYear, int periodMonth)
    {
        var shortTenant = tenantId.Length <= 8 ? tenantId : tenantId[..8];
        return $"INV-{periodYear}{periodMonth:00}-{shortTenant}";
    }
}
