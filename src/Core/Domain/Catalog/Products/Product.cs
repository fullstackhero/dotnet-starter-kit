using FSH.WebApi.Domain.Catalog.Brands;

namespace FSH.WebApi.Domain.Catalog.Products;

public class Product : AuditableEntity, IMustHaveTenant, IAggregateRoot
{
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public decimal Rate { get; private set; }
    public string? Tenant { get; set; }
    public string? ImagePath { get; set; }
    public Guid BrandId { get; set; }
    public virtual Brand Brand { get; set; } = default!;

    public Product(string? name, string? description, decimal rate, in Guid brandId, string? imagePath)
    {
        Name = name;
        Description = description;
        Rate = rate;
        ImagePath = imagePath;
        BrandId = brandId;
    }

    protected Product()
    {
    }

    public Product Update(string? name, string? description, decimal rate, in Guid brandId, string? imagePath)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (Rate != rate) Rate = rate;
        if (brandId != Guid.Empty && !BrandId.Equals(brandId)) BrandId = brandId;
        if (imagePath is not null && ImagePath?.Equals(imagePath) is not true) ImagePath = imagePath;
        return this;
    }
}