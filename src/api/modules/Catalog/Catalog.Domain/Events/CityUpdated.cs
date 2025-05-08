using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record CityUpdated : DomainEvent
{
    public City? City { get; set; }
}
