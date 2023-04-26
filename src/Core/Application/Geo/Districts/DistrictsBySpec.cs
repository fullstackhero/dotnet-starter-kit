using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts;

public class DistrictsByProvinceSpec : Specification<District>
{
    public DistrictsByProvinceSpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.ProvinceId == fatherId);
}

public class DistrictsByTypeSpec : Specification<District>
{
    public DistrictsByTypeSpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.TypeId == fatherId);
}