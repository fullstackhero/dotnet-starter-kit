using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain.Events;

public sealed record CustomerUpdated : DomainEvent
{
    public Customer? Customer { get; set; }
}
