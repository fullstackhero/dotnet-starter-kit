using FSH.Framework.Core.Domain;
using FSH.Framework.Shared.Quota;

namespace FSH.Modules.Billing.Domain;

/// <summary>
/// Priced side of a tenant plan. The plan key matches the key used by quota configuration so a
/// plan named "pro" in QuotaOptions.Plans corresponds to the BillingPlan with Key "pro". Limits
/// come from QuotaOptions; prices and overage rates come from here.
///
/// <see cref="IGlobalEntity"/>: plans are platform-wide catalogue rows, NOT per-tenant.
/// Every tenant subscribes to one of these shared plans.
/// </summary>
public sealed class BillingPlan : BaseEntity<Guid>, IGlobalEntity
{
    private readonly Dictionary<QuotaResource, decimal> _overageRates = new();

    public string Key { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Currency { get; private set; } = "USD";
    public decimal MonthlyBasePrice { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyDictionary<QuotaResource, decimal> OverageRates => _overageRates;

    private BillingPlan() { }

    public static BillingPlan Create(
        string key,
        string name,
        string currency,
        decimal monthlyBasePrice,
        IReadOnlyDictionary<QuotaResource, decimal>? overageRates = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (monthlyBasePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyBasePrice), "Price cannot be negative.");
        }

        var plan = new BillingPlan
        {
            Id = Guid.CreateVersion7(),
#pragma warning disable CA1308 // Plan keys are canonical slugs stored lowercase (not security-sensitive)
            Key = key.ToLowerInvariant(),
#pragma warning restore CA1308
            Name = name,
            Currency = currency.ToUpperInvariant(),
            MonthlyBasePrice = monthlyBasePrice,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (overageRates is not null)
        {
            foreach (var (res, rate) in overageRates)
            {
                plan._overageRates[res] = rate;
            }
        }

        return plan;
    }

    public void Update(string name, decimal monthlyBasePrice, IReadOnlyDictionary<QuotaResource, decimal>? overageRates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (monthlyBasePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyBasePrice), "Price cannot be negative.");
        }

        Name = name;
        MonthlyBasePrice = monthlyBasePrice;
        _overageRates.Clear();
        if (overageRates is not null)
        {
            foreach (var (res, rate) in overageRates)
            {
                _overageRates[res] = rate;
            }
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public decimal GetOverageRate(QuotaResource resource) =>
        _overageRates.TryGetValue(resource, out var rate) ? rate : 0m;
}
