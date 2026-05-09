using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain.Events;

public sealed record MeterReadingCreated : DomainEvent
{
    public MeterReading? MeterReading { get; set; }
}
