using Ardalis.Specification;
using DN.WebApi.Application.Common.Specification;
using DN.WebApi.Domain.Catalog.Products;

namespace DN.WebApi.Application.Catalog.Products;

public class ProductByIdWithBrandsSpec : EntitiesMappedByMapsterSpec<Product, ProductDto>, ISingleResultSpecification
{
    public ProductByIdWithBrandsSpec(Guid id) =>
        Query
            .Where(p => p.Id == id)
            .Include(p => p.Brand);
}