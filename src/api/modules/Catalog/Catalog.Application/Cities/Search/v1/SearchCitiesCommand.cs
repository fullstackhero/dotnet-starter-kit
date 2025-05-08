using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Search.v1;

public class SearchCitiesCommand : PaginationFilter, IRequest<PagedList<CityResponse>>
{
    public Guid? RegionId { get; set; }
    public decimal? MinimumRate { get; set; }
    public decimal? MaximumRate { get; set; }
}
