using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts.v1.Subscriptions;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Subscriptions.AssignSubscription;

public sealed class AssignSubscriptionCommandHandler(
    BillingDbContext dbContext,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<AssignSubscriptionCommand, Guid>
{
    public async ValueTask<Guid> Handle(AssignSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Only the root operator may assign a subscription to an arbitrary tenant. A tenant caller is
        // pinned to its own tenant, so it can't (re)assign or cancel another tenant's subscription by
        // passing a foreign tenant id in the body.
        var callerTenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.");
        var isRoot = callerTenantId == MultitenancyConstants.Root.Id;
        var targetTenantId = isRoot ? command.TenantId : callerTenantId;

#pragma warning disable CA1308 // Plan keys are canonical lowercase slugs
        var key = command.PlanKey.ToLowerInvariant();
#pragma warning restore CA1308
        var plan = await dbContext.Plans.FirstOrDefaultAsync(p => p.Key == key && p.IsActive, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Active plan with key '{command.PlanKey}' not found.");

        var now = DateTime.UtcNow;
        var current = await dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == targetTenantId && s.Status == Contracts.SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        current?.Cancel(now);

        var subscription = Subscription.Create(targetTenantId, plan.Id, now);
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return subscription.Id;
    }
}
