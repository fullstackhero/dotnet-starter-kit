using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Water.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain;

public class MeterReading : AuditableEntity, IAggregateRoot
{
    public Guid MeterId { get; private set; }
    public virtual Meter Meter { get; private set; } = default!;
    public DateTimeOffset ReadingDate { get; private set; }
    public decimal ReadingValue { get; private set; }
    public decimal? PreviousReadingValue { get; private set; }
    public decimal Consumption { get; private set; }
    public ReadingSource Source { get; private set; }
    public string? Notes { get; private set; }

    private MeterReading() { }

    private MeterReading(Guid id, Guid meterId, DateTimeOffset readingDate, decimal readingValue, decimal? previousReadingValue, ReadingSource source, string? notes)
    {
        Id = id;
        MeterId = meterId;
        ReadingDate = readingDate;
        ReadingValue = readingValue;
        PreviousReadingValue = previousReadingValue;
        Consumption = previousReadingValue.HasValue ? readingValue - previousReadingValue.Value : 0;
        Source = source;
        Notes = notes;

        QueueDomainEvent(new MeterReadingCreated { MeterReading = this });
    }

    public static MeterReading Create(Guid meterId, DateTimeOffset readingDate, decimal readingValue, decimal? previousReadingValue, ReadingSource source, string? notes)
    {
        return new MeterReading(Guid.NewGuid(), meterId, readingDate, readingValue, previousReadingValue, source, notes);
    }
}
