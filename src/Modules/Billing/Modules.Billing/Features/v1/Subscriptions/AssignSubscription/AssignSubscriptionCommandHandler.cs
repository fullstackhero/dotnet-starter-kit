using FSH.Framework.Core.Exceptions;
using FSH.Modules.Billing.Contracts.v1.Subscriptions;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Subscriptions.AssignSubscription;

public sealed class AssignSubscriptionCommandHandler(BillingDbContext dbContext)
    : ICommandHandler<AssignSubscriptionCommand, Guid>
{
    public async ValueTask<Guid> Handle(AssignSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

#pragma warning disable CA1308 // Plan keys are canonical lowercase slugs
        var key = command.PlanKey.ToLowerInvariant();
#pragma warning restore CA1308
        var plan = await dbContext.Plans.FirstOrDefaultAsync(p => p.Key == key && p.IsActive, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Active plan with key '{command.PlanKey}' not found.");

        var now = DateTime.UtcNow;
        var current = await dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == command.TenantId && s.Status == Contracts.SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        current?.Cancel(now);

        var subscription = Subscription.Create(command.TenantId, plan.Id, now);
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return subscription.Id;
    }
}
