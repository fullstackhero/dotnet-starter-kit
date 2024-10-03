using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Export.v1;

public class ExportProductsRequest : BaseFilter, IRequest<byte[]>
{
    public Guid? BrandId { get; set; }
    public decimal? MinimumRate { get; set; }
    public decimal? MaximumRate { get; set; }
}
