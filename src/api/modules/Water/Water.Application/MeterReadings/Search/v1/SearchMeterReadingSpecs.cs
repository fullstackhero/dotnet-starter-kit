using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Water.Application.MeterReadings.Get.v1;
using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Search.v1;

public class SearchMeterReadingSpecs : EntitiesByPaginationFilterSpec<MeterReading, MeterReadingResponse>
{
    public SearchMeterReadingSpecs(SearchMeterReadingsCommand command)
        : base(command) =>
        Query
            .OrderByDescending(c => c.ReadingDate, !command.HasOrderBy())
            .Where(b => b.MeterId == command.MeterId, command.MeterId.HasValue)
            .Where(b => b.Source == command.Source, command.Source.HasValue);
}
