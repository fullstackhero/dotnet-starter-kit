using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Water.Application.Meters.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Meters.Search.v1;

public class SearchMetersCommand : PaginationFilter, IRequest<PagedList<MeterResponse>>
{
    public string? MeterNumber { get; set; }
    public string? Model { get; set; }
    public MeterStatus? Status { get; set; }
}
