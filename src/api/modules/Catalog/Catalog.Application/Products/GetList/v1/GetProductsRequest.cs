using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.GetList.v1;

public class GetProductsRequest : BaseFilter, IRequest<List<ProductDto>>
{
    public Guid? BrandId { get; set; }
    public decimal? MinimumRate { get; set; }
    public decimal? MaximumRate { get; set; }
}
