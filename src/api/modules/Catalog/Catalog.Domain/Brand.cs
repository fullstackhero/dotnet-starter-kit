using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Catalog.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain;
public class Brand : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public static Brand Create(string name, string? description)
    {
        var brand = new Brand
        {
            Name = name,
            Description = description
        };

        brand.QueueDomainEvent(new BrandCreated() { Brand = brand });

        return brand;
    }

    public Brand Update(string? name, string? description)
    {
        if (name is not null && Name?.Equals(name, StringComparison.OrdinalIgnoreCase) is not true) Name = name;
        if (description is not null && Description?.Equals(description, StringComparison.OrdinalIgnoreCase) is not true) Description = description;

        this.QueueDomainEvent(new BrandUpdated() { Brand = this });

        return this;
    }

    public static Brand Update(Guid id, string name, string? description)
    {
        var brand = new Brand
        {
            Id = id,
            Name = name,
            Description = description
        };

        brand.QueueDomainEvent(new BrandUpdated() { Brand = brand });

        return brand;
    }
}

