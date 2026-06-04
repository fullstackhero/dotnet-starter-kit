using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Services;
using FSH.Modules.Multitenancy.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Billing.IntegrationEventHandlers;

/// <summary>
/// Reacts to a tenant renewal: when the plan changed, swaps the active subscription to the new plan;
/// either way issues the new term's subscription invoice (idempotent, guarded by invoice number).
/// </summary>
public sealed class TenantRenewedIntegrationEventHandler(
    BillingDbContext db,
    IBillingService billing,
    ILogger<TenantRenewedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<TenantRenewedIntegrationEvent>
{
    public async Task HandleAsync(TenantRenewedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var tenantId = @event.TenantId
            ?? throw new InvalidOperationException("TenantRenewedIntegrationEvent is missing TenantId.");

        if (@event.PlanChanged)
        {
            await TenantSubscriptionMaintenance.ReplaceActiveSubscriptionAsync(
                db, tenantId, @event.PlanId, @event.PeriodStartUtc, @event.PeriodEndUtc, ct).ConfigureAwait(false);
        }
        else
        {
            // Same-plan renewal: extend the active subscription's term so EndUtc tracks the renewed
            // ValidUpto (otherwise the dashboard's "Current term"/validity drifts behind enforcement).
            await TenantSubscriptionMaintenance.ExtendActiveSubscriptionAsync(
                db, tenantId, @event.PeriodEndUtc, ct).ConfigureAwait(false);
        }

        await billing.CreateSubscriptionInvoiceAsync(
            tenantId, @event.PlanId, @event.PeriodStartUtc, @event.PeriodEndUtc, ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Billing] tenant {TenantId} renewed on plan {PlanKey} (planChanged={PlanChanged}); term ends {End:o}",
                tenantId, @event.PlanKey, @event.PlanChanged, @event.PeriodEndUtc);
        }
    }
}
