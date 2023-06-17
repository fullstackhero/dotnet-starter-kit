namespace FL_CRMS_ERP_WEBAPI.Application.Catalog.Products;

public class ProductByIdWithBrandSpec : Specification<Product, ProductDetailsDto>, ISingleResultSpecification
{
    public ProductByIdWithBrandSpec(Guid id) =>
        Query
            .Where(p => p.Id == id)
            .Include(p => p.Brand);
}