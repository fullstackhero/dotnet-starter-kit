using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.GeoAdminUnits;

public class GeoAdminUnitByIdSpec : Specification<GeoAdminUnit, GeoAdminUnitDetailsDto>, ISingleResultSpecification
{
    public GeoAdminUnitByIdSpec(DefaultIdType id) =>
        Query
            .Where(e => e.Id == id);
}

public class GeoAdminUnitByCodeSpec : Specification<GeoAdminUnit>, ISingleResultSpecification
{
    public GeoAdminUnitByCodeSpec(string code) =>
        Query
            .Where(e => e.Code == code);
}

public class GeoAdminUnitByNameSpec : Specification<GeoAdminUnit>, ISingleResultSpecification
{
    public GeoAdminUnitByNameSpec(string name) =>
        Query
            .Where(e => e.Name == name);
}