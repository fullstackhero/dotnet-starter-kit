namespace FSH.WebApi.Domain.Geo;
public class Region : AuditableEntity, IAggregateRoot
{
    public int Order { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; set; }

    public string? FullName { get; private set; }
    public string? NativeName { get; private set; }
    public string? FullNativeName { get; private set; }
    public int? NumericCode { get; private set; }

    public string Type { get; private set; } = default!;
    public string Latitude { get; private set; } = default!;
    public string Longitude { get; private set; } = default!;

    public string Metropolis { get; private set; } = default!;

    // cpublic DefaultIdType? MetropolisId { get; private set; }
    // cpublic virtual Province? Metropolis { get; private set; }

    public DefaultIdType CountryId { get; private set; } = default!;
    public virtual Country Country { get; private set; } = default!;

   // public virtual ICollection<Province> Provinces { get; private set; } = default!;
}