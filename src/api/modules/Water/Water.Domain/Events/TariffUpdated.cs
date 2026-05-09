using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain.Events;

public sealed record TariffUpdated : DomainEvent
{
    public Tariff? Tariff { get; set; }
}
