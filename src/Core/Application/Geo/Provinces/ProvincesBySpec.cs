using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces;

public class ProvincesByStateSpec : Specification<Province>
{
    public ProvincesByStateSpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.StateId == fatherId);
}

public class ProvincesByTypeSpec : Specification<Province>
{
    public ProvincesByTypeSpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.TypeId == fatherId);
}