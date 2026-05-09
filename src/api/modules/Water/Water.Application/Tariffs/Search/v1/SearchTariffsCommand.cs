using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Water.Application.Tariffs.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Search.v1;

public class SearchTariffsCommand : PaginationFilter, IRequest<PagedList<TariffResponse>>
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
