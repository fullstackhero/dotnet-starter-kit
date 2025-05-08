using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record CityCreated : DomainEvent
{
    public City? City { get; set; }

}
