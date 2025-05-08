using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record ReviewUpdated : DomainEvent
{
    public Review? Review { get; set; }
}
