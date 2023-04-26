namespace FSH.WebApi.Domain.Geo;
public class Province : AuditableEntity, IAggregateRoot
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

    public string? Latitude { get; private set; }
    public string? Longitude { get; private set; }

    public string? Metropolis { get; private set; }

    public string? ZipCode { get; private set; }
    public string? PhoneCode { get; private set; }

    public int? Population { get; private set; }
    public decimal? Area { get; private set; }

    public string? WikiDataId { get; private set; }

    public DefaultIdType? TypeId { get; private set; }
    public virtual GeoAdminUnit? Type { get; private set; }

    public DefaultIdType StateId { get; private set; } = default!;
    public virtual State State { get; private set; } = default!;

    public virtual ICollection<District> Districts { get; private set; } = default!;

    public Province(
    int order,
    string code,
    string name,
    string? description,
    bool isActive,
    string? fullName,
    string? nativeName,
    string? fullNativeName,
    int? numericCode,
    string? latitude,
    string? longitude,
    string? metropolis,
    string? zipCode,
    string? phoneCode,
    int? population,
    decimal? area,
    string? wikiDataId,
    DefaultIdType? typeId,
    DefaultIdType stateId)
    {
        Order = order;
        Code = code;
        Name = name;
        Description = description ?? string.Empty;
        IsActive = isActive;

        FullName = fullName;
        NativeName = nativeName;
        FullNativeName = fullNativeName;
        NumericCode = numericCode ?? 0;

        Latitude = latitude;
        Longitude = longitude;
        Metropolis = metropolis;

        ZipCode = zipCode;
        PhoneCode = phoneCode;
        Population = population ?? 0;
        Area = area ?? 0;
        WikiDataId = wikiDataId;

        TypeId = (typeId == DefaultIdType.Empty) ? null : typeId;
        StateId = stateId;
    }

    public Province()
        : this(
                0,
                string.Empty,
                string.Empty,
                null,
                true,
                null,
                null,
                null,
                0,
                null,
                null,
                null,
                null,
                null,
                0,
                0,
                null,
                null,
                DefaultIdType.Empty)
    {
    }

    public Province Update(
        int? order,
        string? code,
        string? name,
        string? description,
        bool? isActive,
        string? fullName,
        string? nativeName,
        string? fullNativeName,
        int? numericCode,
        string? latitude,
        string? longitude,
        string? metropolis,
        string? zipCode,
        string? phoneCode,
        int? population,
        decimal? area,
        string? wikiDataId,
        DefaultIdType? typeId,
        DefaultIdType? stateId)
    {
        if (order is not null && order.HasValue && Order != order) Order = order.Value;
        if (code is not null && Code?.Equals(code) is not true) Code = code;
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (isActive is not null && !IsActive.Equals(isActive)) IsActive = (bool)isActive;

        if (fullName is not null && FullName?.Equals(fullName) is not true) FullName = fullName;
        if (nativeName is not null && NativeName?.Equals(nativeName) is not true) NativeName = nativeName;
        if (fullNativeName is not null && FullNativeName?.Equals(fullNativeName) is not true) FullNativeName = fullNativeName;
        if (numericCode is not null && numericCode.HasValue && NumericCode != numericCode) NumericCode = numericCode.Value;

        if (latitude is not null && Latitude?.Equals(latitude) is not true) Latitude = latitude;
        if (longitude is not null && Longitude?.Equals(longitude) is not true) Longitude = longitude;
        if (metropolis is not null && Metropolis?.Equals(metropolis) is not true) Metropolis = metropolis;

        if (zipCode is not null && ZipCode?.Equals(zipCode) is not true) ZipCode = zipCode;
        if (phoneCode is not null && PhoneCode?.Equals(phoneCode) is not true) PhoneCode = phoneCode;

        if (population is not null && population.HasValue && Population != population) Population = population.Value;
        if (area is not null && area.HasValue && Area != area) Area = area.Value;
        if (wikiDataId is not null && WikiDataId?.Equals(wikiDataId) is not true) WikiDataId = wikiDataId;

        if (typeId.HasValue && typeId.Value != DefaultIdType.Empty && !TypeId.Equals(typeId.Value)) TypeId = typeId.Value;
        if (stateId.HasValue && stateId.Value != DefaultIdType.Empty && !StateId.Equals(stateId.Value)) StateId = stateId.Value;

        return this;
    }

}