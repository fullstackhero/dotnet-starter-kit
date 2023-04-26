namespace FSH.WebApi.Application.Geo.Provinces;

public class ProvinceDto : IDto
{
    public DefaultIdType Id { get; set; }

    public int Order { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public string? FullName { get; set; }
    public string? NativeName { get; set; }
    public string? FullNativeName { get; set; }
    public int? NumericCode { get; set; }

    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Metropolis { get; set; }

    public string? ZipCode { get; set; }
    public string? PhoneCode { get; set; }

    public int? Population { get; set; }
    public decimal? Area { get; set; }

    public string? WikiDataId { get; set; }

    public DefaultIdType? TypeId { get; set; }
    public string? TypeName { get; set; }
    public string? TypeNativeName { get; set; }

    public DefaultIdType StateId { get; set; }
    public string StateName { get; set; } = default!;
    public string StateNativeName { get; set; } = default!;
}

public class ProvinceDetailsDto : IDto
{
    public DefaultIdType Id { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? NativeName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ProvinceExportDto : IDto
{
    public DefaultIdType Id { get; set; }

    public int Order { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public string? FullName { get; set; }
    public string? NativeName { get; set; }
    public string? FullNativeName { get; set; }
    public int? NumericCode { get; set; }

    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Metropolis { get; set; }

    public string? ZipCode { get; set; }
    public string? PhoneCode { get; set; }

    public int? Population { get; set; }
    public decimal? Area { get; set; }

    public string? WikiDataId { get; set; }

    public DefaultIdType? TypeId { get; set; }
    public string? TypeName { get; set; }

    public DefaultIdType StateId { get; set; }
    public string StateName { get; set; } = default!;

    public DefaultIdType CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DefaultIdType LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}