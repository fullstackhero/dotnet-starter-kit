using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Domain.Entities.Catalog.Events;

public class ProductDeletedEvent : DomainEvent
{
    public ProductDeletedEvent(Product product)
    {
        Product = product;
    }

    public Product Product { get; }
}