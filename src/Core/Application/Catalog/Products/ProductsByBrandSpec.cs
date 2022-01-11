using Ardalis.Specification;
using DN.WebApi.Domain.Catalog.Products;

namespace DN.WebApi.Application.Catalog.Products;

public class ProductsByBrandSpec : Specification<Product>
{
    public ProductsByBrandSpec(Guid brandId) =>
        Query.Where(p => p.BrandId == brandId);
}
