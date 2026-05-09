using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Water.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain;

public class Meter : AuditableEntity, IAggregateRoot
{
    public string MeterNumber { get; private set; } = string.Empty;
    public string? Model { get; private set; }
    public DateTimeOffset InstallationDate { get; private set; }
    public DateTimeOffset? LastReadingDate { get; private set; }
    public MeterStatus Status { get; private set; }
    public Guid CustomerId { get; private set; }
    public virtual Customer Customer { get; private set; } = default!;

    private Meter() { }

    private Meter(Guid id, string meterNumber, string? model, DateTimeOffset installationDate, Guid customerId)
    {
        Id = id;
        MeterNumber = meterNumber;
        Model = model;
        InstallationDate = installationDate;
        CustomerId = customerId;
        Status = MeterStatus.Active;

        QueueDomainEvent(new MeterCreated { Meter = this });
    }

    public static Meter Create(string meterNumber, string? model, DateTimeOffset installationDate, Guid customerId)
    {
        return new Meter(Guid.NewGuid(), meterNumber, model, installationDate, customerId);
    }

    public Meter Update(string? model, MeterStatus? status, DateTimeOffset? lastReadingDate)
    {
        bool isUpdated = false;

        if (!string.Equals(Model, model, StringComparison.OrdinalIgnoreCase))
        {
            Model = model;
            isUpdated = true;
        }

        if (status.HasValue && Status != status.Value)
        {
            Status = status.Value;
            isUpdated = true;
        }

        if (lastReadingDate.HasValue && LastReadingDate != lastReadingDate.Value)
        {
            LastReadingDate = lastReadingDate.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new MeterUpdated { Meter = this });
        }

        return this;
    }
}
