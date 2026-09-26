using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Contracts.v1.Subscriptions;
using FSH.Modules.Billing.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Subscriptions.GetSubscription;

public sealed class GetSubscriptionQueryHandler(
    BillingDbContext dbContext,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : IQueryHandler<GetSubscriptionQuery, SubscriptionDto?>
{
    public async ValueTask<SubscriptionDto?> Handle(GetSubscriptionQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var callerTenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.")
            {
                MessageKey = "Error.TenantContextRequired",
            };

        // BillingDbContext is not tenant-filtered, so a tenant caller is pinned to its OWN
        // subscription and only root may pass an arbitrary tenant id (else cross-tenant reads).
        var tenantId = callerTenantId == MultitenancyConstants.Root.Id
            ? query.TenantId ?? callerTenantId
            : callerTenantId;

        var sub = await (from s in dbContext.Subscriptions.AsNoTracking()
                         join p in dbContext.Plans.AsNoTracking() on s.PlanId equals p.Id
                         where s.TenantId == tenantId
                            && s.Status == Contracts.SubscriptionStatus.Active
                         select new SubscriptionDto(s.Id, s.TenantId, s.PlanId, p.Key, s.StartUtc, s.EndUtc, s.Status))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return sub;
    }
}
