using DN.WebApi.Domain.Common.Contracts;

namespace DN.WebApi.Domain.Catalog.Products;

public class ProductCreatedEvent : DomainEvent
{
    public ProductCreatedEvent(Product product) => Product = product;

    public Product Product { get; }
}