namespace FSH.WebApi.Application.Geo.States;

public class StateDto : IDto
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

    public DefaultIdType? TypeId { get; set; }
    public string? TypeName { get; set; }

    public DefaultIdType CountryId { get; set; }
    public string CountryName { get; set; } = default!;
}

public class StateDetailsDto : IDto
{
    public DefaultIdType Id { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? NativeName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class StateExportDto : IDto
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

    public DefaultIdType? TypeId { get; set; }
    public string? TypeName { get; set; }

    public DefaultIdType CountryId { get; set; }
    public string CountryName { get; set; } = default!;

    public DefaultIdType CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DefaultIdType LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}