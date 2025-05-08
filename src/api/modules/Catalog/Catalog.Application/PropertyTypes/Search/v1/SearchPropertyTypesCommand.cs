using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Search.v1;

public class SearchPropertyTypesCommand : PaginationFilter, IRequest<PagedList<PropertyTypeResponse>>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
