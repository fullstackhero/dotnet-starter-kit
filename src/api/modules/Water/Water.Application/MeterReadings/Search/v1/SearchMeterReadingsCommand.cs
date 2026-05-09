using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Water.Application.MeterReadings.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Search.v1;

public class SearchMeterReadingsCommand : PaginationFilter, IRequest<PagedList<MeterReadingResponse>>
{
    public Guid? MeterId { get; set; }
    public ReadingSource? Source { get; set; }
}
