using DN.WebApi.Shared.DTOs.Filters;
using System;

namespace DN.WebApi.Shared.DTOs.Catalog
{
    public class ProductListFilter : PaginationFilter
    {
        public Guid? BrandId { get; set; }
        public decimal? MinimumRate { get; set; }
        public decimal? MaximumRate { get; set; }
    }
}