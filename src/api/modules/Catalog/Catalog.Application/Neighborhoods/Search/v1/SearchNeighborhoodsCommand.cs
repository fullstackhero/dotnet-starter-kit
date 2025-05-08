using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Search.v1;

public class SearchNeighborhoodsCommand : PaginationFilter, IRequest<PagedList<NeighborhoodResponse>>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}