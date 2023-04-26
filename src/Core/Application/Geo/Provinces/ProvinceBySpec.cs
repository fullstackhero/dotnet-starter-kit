using FSH.WebApi.Domain.Geo;

namespace FSH.WebApi.Application.Geo.Provinces;

public class ProvinceByIdSpec : Specification<Province, ProvinceDetailsDto>, ISingleResultSpecification
{
    public ProvinceByIdSpec(DefaultIdType id) =>
        Query
            .Where(e => e.Id == id);
}

public class ProvinceByCodeSpec : Specification<Province>, ISingleResultSpecification
{
    public ProvinceByCodeSpec(string code) =>
        Query
            .Where(e => e.Code == code);
}

public class ProvinceByNameSpec : Specification<Province>, ISingleResultSpecification
{
    public ProvinceByNameSpec(string name) =>
        Query
            .Where(e => e.Name == name);
}