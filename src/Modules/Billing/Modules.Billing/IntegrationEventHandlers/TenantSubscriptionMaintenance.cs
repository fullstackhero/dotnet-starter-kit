using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.IntegrationEventHandlers;

/// <summary>
/// Shared subscription bookkeeping for the tenant-lifecycle integration handlers: at most one active
/// subscription per tenant, so starting a new one cancels the current.
/// </summary>
internal static class TenantSubscriptionMaintenance
{
    public static async Task ReplaceActiveSubscriptionAsync(
        BillingDbContext db,
        string tenantId,
        Guid planId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var active = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        active?.Cancel(startUtc);

        db.Subscriptions.Add(Subscription.Create(tenantId, planId, startUtc, endUtc));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
