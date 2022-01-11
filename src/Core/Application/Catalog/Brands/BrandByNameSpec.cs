using Ardalis.Specification;
using DN.WebApi.Domain.Catalog.Brands;

namespace DN.WebApi.Application.Catalog.Brands;

public class BrandByNameSpec : Specification<Brand>, ISingleResultSpecification
{
    public BrandByNameSpec(string name) =>
        Query.Where(b => b.Name == name);
}