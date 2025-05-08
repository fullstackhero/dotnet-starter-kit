using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Catalog.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain;
public class Neighborhood : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CityId { get; private set; }
    public virtual City City { get; private set; } = default!;
    public string SphereImgURL { get; private set; } = string.Empty;
    public double Score { get; private set; }

    private Neighborhood() { }

    private Neighborhood(Guid id, string name, string description, Guid cityId, string sphereImgURL, double score)
    {
        Id = id;
        Name = name;
        Description = description;
        CityId = cityId;
        SphereImgURL = sphereImgURL;
        Score = score;

        QueueDomainEvent(new NeighborhoodCreated { Neighborhood = this });
    }

    public static Neighborhood Create(string name, string description, Guid cityId, string sphereImgURL, double score)
    {
        return new Neighborhood(Guid.NewGuid(), name, description, cityId, sphereImgURL, score);
    }

    public Neighborhood Update(string? name, string? description, Guid? cityId, string? sphereImgURL, double? score)
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

        if (cityId.HasValue && CityId != cityId.Value)
        {
            CityId = cityId.Value;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(sphereImgURL) && !string.Equals(SphereImgURL, sphereImgURL, StringComparison.OrdinalIgnoreCase))
        {
            SphereImgURL = sphereImgURL;
            isUpdated = true;
        }

        if (score.HasValue && Score != score.Value)
        {
            Score = score.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new NeighborhoodUpdated { Neighborhood = this });
        }

        return this;
    }
}
