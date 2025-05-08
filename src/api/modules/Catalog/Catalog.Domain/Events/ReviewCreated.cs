using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record ReviewCreated : DomainEvent
{
    public Review? Review { get; set; }
}
