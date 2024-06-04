using FSH.Framework.Core.Domain.Events;

namespace FSH.WebApi.Catalog.Domain.Events;
public sealed record ProductRead : DomainEvent
{
    public Product? Product { get; set; }
}
