using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Domain.Entities.Catalog.Events;

public class ProductUpdatedEvent : DomainEvent
{
    public ProductUpdatedEvent(Product product)
    {
        Product = product;
    }

    public Product Product { get; }
}