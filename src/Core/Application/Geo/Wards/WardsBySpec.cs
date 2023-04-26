using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Wards;

public class WardsByDistrictSpec : Specification<Ward>
{
    public WardsByDistrictSpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.DistrictId == fatherId);
}

public class WardsByTypeSpec : Specification<Ward>
{
    public WardsByTypeSpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.TypeId == fatherId);
}