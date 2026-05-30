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

    /// <summary>
    /// Same-plan renewal: extend the active subscription's end so <c>Subscription.EndUtc</c> stays in
    /// step with the tenant's renewed <c>ValidUpto</c> (otherwise the dashboard's subscription term
    /// drifts behind the enforced validity). Idempotent via <see cref="Subscription.Extend"/>.
    /// </summary>
    public static async Task ExtendActiveSubscriptionAsync(
        BillingDbContext db,
        string tenantId,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var active = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        if (active is null)
        {
            return;
        }

        active.Extend(endUtc);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
