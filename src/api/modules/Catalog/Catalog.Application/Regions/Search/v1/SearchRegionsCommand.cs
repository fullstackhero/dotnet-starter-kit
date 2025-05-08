using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Regions.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Search.v1;

public class SearchRegionsCommand : PaginationFilter, IRequest<PagedList<RegionResponse>>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}