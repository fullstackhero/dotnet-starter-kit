using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;

public class SearchProductsRequest : PaginationFilter, IRequest<PagedList<ProductDto>>
{
    public Guid? BrandId { get; set; }
    public decimal? MinimumRate { get; set; }
    public decimal? MaximumRate { get; set; }
}
