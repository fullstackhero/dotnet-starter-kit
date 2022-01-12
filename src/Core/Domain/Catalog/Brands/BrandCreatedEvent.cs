namespace FSH.WebApi.Domain.Catalog.Brands;

public class BrandCreatedEvent : DomainEvent
{
    public BrandCreatedEvent(Brand brand) => Brand = brand;

    public Brand Brand { get; }
}