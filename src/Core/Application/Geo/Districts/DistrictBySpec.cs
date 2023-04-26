using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Districts;

public class DistrictByIdSpec : Specification<District, DistrictDetailsDto>, ISingleResultSpecification
{
    public DistrictByIdSpec(DefaultIdType id) =>
        Query
            .Where(e => e.Id == id);
}

public class DistrictByCodeSpec : Specification<District>, ISingleResultSpecification
{
    public DistrictByCodeSpec(string code) =>
        Query
            .Where(e => e.Code == code);
}

public class DistrictByNameSpec : Specification<District>, ISingleResultSpecification
{
    public DistrictByNameSpec(string name) =>
        Query
            .Where(e => e.Name == name);
}