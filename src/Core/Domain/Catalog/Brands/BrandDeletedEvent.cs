namespace FSH.WebApi.Domain.Catalog.Brands;

public class BrandDeletedEvent : DomainEvent
{
    public BrandDeletedEvent(Brand brand) => Brand = brand;

    public Brand Brand { get; }
}