using DN.WebApi.Domain.Common.Contracts;

namespace DN.WebApi.Domain.Catalog.Brands;

public class BrandUpdatedEvent : DomainEvent
{
    public BrandUpdatedEvent(Brand brand) => Brand = brand;

    public Brand Brand { get; }
}