using FSH.Framework.Core.Domain;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

/// <summary>
/// Binds a tenant to a billing plan over a time window. At most one subscription is Active per
/// tenant at a time — assignment replaces the prior active subscription (which is Cancelled with
/// EndUtc set to the new one's StartUtc).
/// </summary>
public sealed class Subscription : BaseEntity<Guid>
{
    public string TenantId { get; private set; } = default!;
    public Guid PlanId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Subscription() { }

    public static Subscription Create(string tenantId, Guid planId, DateTime startUtc)
        => Create(tenantId, planId, startUtc, endUtc: null);

    public static Subscription Create(string tenantId, Guid planId, DateTime startUtc, DateTime? endUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (planId == Guid.Empty)
        {
            throw new ArgumentException("PlanId is required.", nameof(planId));
        }

        return new Subscription
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            PlanId = planId,
            StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            EndUtc = endUtc is { } e ? DateTime.SpecifyKind(e, DateTimeKind.Utc) : null,
            Status = SubscriptionStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Suspend()
    {
        Status = SubscriptionStatus.Suspended;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        Status = SubscriptionStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(DateTime endUtc)
    {
        Status = SubscriptionStatus.Cancelled;
        EndUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
