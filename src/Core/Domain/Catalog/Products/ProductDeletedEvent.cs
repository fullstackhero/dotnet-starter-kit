namespace FSH.WebApi.Domain.Catalog.Products;

public class ProductDeletedEvent : DomainEvent
{
    public ProductDeletedEvent(Product product) => Product = product;

    public Product Product { get; }
}