namespace FSH.WebApi.Application.Catalog.Products;

public class ProductsBySearchRequestWithBrandsSpec : EntitiesByPaginationFilterSpec<Product, ProductDto>
{
    public ProductsBySearchRequestWithBrandsSpec(SearchProductsRequest request)
        : base(request)
    {
        Query.Include(p => p.Brand);

        if (request.BrandId.HasValue)
        {
            Query.Where(p => p.BrandId.Equals(request.BrandId.Value));
        }

        if (request.MinimumRate.HasValue)
        {
            Query.Where(p => p.Rate >= request.MinimumRate.Value);
        }

        if (request.MaximumRate.HasValue)
        {
            Query.Where(p => p.Rate <= request.MaximumRate.Value);
        }
    }
}