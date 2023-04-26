using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Countries;

public class CountriesByTypesSpec : Specification<Country>
{
    public CountriesByTypesSpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.TypeId == fatherId ||
                        e.SubTypeId == fatherId ||
                        e.ContinentId == fatherId ||
                        e.SubContinentId == fatherId);
}