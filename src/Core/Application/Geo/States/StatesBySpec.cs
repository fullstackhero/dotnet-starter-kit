using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.States;

public class StatesByCountrySpec : Specification<State>
{
    public StatesByCountrySpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.CountryId == fatherId);
}

public class StatesByTypeSpec : Specification<State>
{
    public StatesByTypeSpec(DefaultIdType fatherId) =>
        Query
            .Where(e => e.TypeId == fatherId);
}