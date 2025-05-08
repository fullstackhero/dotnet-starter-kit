using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record NeighborhoodCreated : DomainEvent
{
    public Neighborhood? Neighborhood { get; set; }
}
