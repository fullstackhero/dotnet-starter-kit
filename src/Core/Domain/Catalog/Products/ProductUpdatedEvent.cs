namespace FSH.WebAPI.Domain.Catalog.Products;

public class ProductUpdatedEvent : DomainEvent
{
    public ProductUpdatedEvent(Product product) => Product = product;

    public Product Product { get; }
}