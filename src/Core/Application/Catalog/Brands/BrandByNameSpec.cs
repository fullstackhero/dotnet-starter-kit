namespace FSH.WebApi.Application.Catalog.Brands;

public class BrandByNameSpec : Specification<Brand>, ISingleResultSpecification
{
    public BrandByNameSpec(string name) =>
        Query.Where(b => b.Name == name);
}