using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;

public class GeoAdminUnitDto : IDto
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
    public int Grade { get; set; }
    public GeoAdminUnitType Type { get; set; }
}

public class GeoAdminUnitDetailsDto : IDto
{
    public DefaultIdType Id { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? NativeName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class GeoAdminUnitExportDto : IDto
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
    public int Grade { get; set; }
    public GeoAdminUnitType Type { get; set; }

    public DefaultIdType CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DefaultIdType LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}