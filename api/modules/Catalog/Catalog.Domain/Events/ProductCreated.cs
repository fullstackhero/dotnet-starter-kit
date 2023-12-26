using FSH.Framework.Core.Domain.Events;

namespace FSH.WebApi.Catalog.Domain.Events;
public sealed class ProductCreated : DomainEvent
{
    public Product? Product { get; set; }
}
