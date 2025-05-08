using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record RegionUpdated : DomainEvent
{
    public Region? Region { get; set; }
}
