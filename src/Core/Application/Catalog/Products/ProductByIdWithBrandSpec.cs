using Ardalis.Specification;
using DN.WebApi.Domain.Catalog.Products;

namespace DN.WebApi.Application.Catalog.Products;

public class ProductByIdWithBrandSpec : Specification<Product, ProductDto>, ISingleResultSpecification
{
    public ProductByIdWithBrandSpec(Guid id) =>
        Query
            .Where(p => p.Id == id)
            .Include(p => p.Brand);
}