using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain.Events;

public sealed record MeterCreated : DomainEvent
{
    public Meter? Meter { get; set; }
}
