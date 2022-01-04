using DN.WebApi.Shared.DTOs.Filters;

namespace DN.WebApi.Application.Catalog.Products;

public class ProductListFilter : PaginationFilter
{
    public Guid? BrandId { get; set; }
    public decimal? MinimumRate { get; set; }
    public decimal? MaximumRate { get; set; }
}