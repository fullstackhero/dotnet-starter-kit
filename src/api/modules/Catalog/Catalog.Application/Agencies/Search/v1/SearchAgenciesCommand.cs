using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Search.v1;

public class SearchAgenciesCommand : PaginationFilter, IRequest<PagedList<AgencyResponse>>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
