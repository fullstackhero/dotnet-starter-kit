using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Domain.Entities.Catalog.Events
{
    public class ProductCreatedEvent : DomainEvent
    {
        public ProductCreatedEvent(Product product)
        {
            Product = product;
        }

        public Product Product { get; }
    }
}