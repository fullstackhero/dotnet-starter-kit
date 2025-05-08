using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Properties.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Search.v1;

public class SearchPropertiesCommand : PaginationFilter, IRequest<PagedList<PropertyResponse>>
{
    public string? Keyword { get; set; }
}
