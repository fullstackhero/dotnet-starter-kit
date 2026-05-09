using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Water.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain;

public class Tariff : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTimeOffset EffectiveDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public decimal RatePerUnit { get; private set; }
    public decimal FixedCharge { get; private set; }
    public bool IsActive { get; private set; }

    private Tariff() { }

    private Tariff(Guid id, string name, string? description, DateTimeOffset effectiveDate, DateTimeOffset? endDate, decimal ratePerUnit, decimal fixedCharge)
    {
        Id = id;
        Name = name;
        Description = description;
        EffectiveDate = effectiveDate;
        EndDate = endDate;
        RatePerUnit = ratePerUnit;
        FixedCharge = fixedCharge;
        IsActive = true;

        QueueDomainEvent(new TariffCreated { Tariff = this });
    }

    public static Tariff Create(string name, string? description, DateTimeOffset effectiveDate, DateTimeOffset? endDate, decimal ratePerUnit, decimal fixedCharge)
    {
        return new Tariff(Guid.NewGuid(), name, description, effectiveDate, endDate, ratePerUnit, fixedCharge);
    }

    public Tariff Update(string? name, string? description, DateTimeOffset? effectiveDate, DateTimeOffset? endDate, decimal? ratePerUnit, decimal? fixedCharge, bool? isActive)
    {
        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(name) && !string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
        {
            Name = name;
            isUpdated = true;
        }

        if (!string.Equals(Description, description, StringComparison.OrdinalIgnoreCase))
        {
            Description = description;
            isUpdated = true;
        }

        if (effectiveDate.HasValue && EffectiveDate != effectiveDate.Value)
        {
            EffectiveDate = effectiveDate.Value;
            isUpdated = true;
        }

        if (EndDate != endDate)
        {
            EndDate = endDate;
            isUpdated = true;
        }

        if (ratePerUnit.HasValue && RatePerUnit != ratePerUnit.Value)
        {
            RatePerUnit = ratePerUnit.Value;
            isUpdated = true;
        }

        if (fixedCharge.HasValue && FixedCharge != fixedCharge.Value)
        {
            FixedCharge = fixedCharge.Value;
            isUpdated = true;
        }

        if (isActive.HasValue && IsActive != isActive.Value)
        {
            IsActive = isActive.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new TariffUpdated { Tariff = this });
        }

        return this;
    }
}
