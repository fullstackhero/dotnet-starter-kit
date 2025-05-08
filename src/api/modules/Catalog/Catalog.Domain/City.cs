using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Catalog.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain;
public class City : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid RegionId { get; private set; }
    public virtual Region Region { get; private set; } = default!;

    private City() { }

    private City(Guid id, string name, string description, Guid regionId)
    {
        Id = id;
        Name = name;
        Description = description;
        RegionId = regionId;

        QueueDomainEvent(new CityCreated { City = this });

    }

    public static City Create(string name, string description, Guid regionId)
    {
        return new City(Guid.NewGuid(), name, description, regionId);
    }

    public City Update(string? name, string? description, Guid? regionId)
    {
        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(name) && !string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
        {
            Name = name;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(description) && !string.Equals(Description, description, StringComparison.OrdinalIgnoreCase))
        {
            Description = description;
            isUpdated = true;
        }

        if (regionId.HasValue && RegionId != regionId.Value)
        {
            RegionId = regionId.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new CityUpdated { City = this });
        }

        return this;
    }
}
