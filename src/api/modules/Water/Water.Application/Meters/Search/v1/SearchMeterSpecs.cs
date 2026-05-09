using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Water.Application.Meters.Get.v1;
using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Meters.Search.v1;

public class SearchMeterSpecs : EntitiesByPaginationFilterSpec<Meter, MeterResponse>
{
    public SearchMeterSpecs(SearchMetersCommand command)
        : base(command) =>
        Query
            .OrderBy(c => c.MeterNumber, !command.HasOrderBy())
            .Where(b => b.MeterNumber.Contains(command.Keyword), !string.IsNullOrEmpty(command.Keyword))
            .Where(b => b.Model!.Contains(command.Keyword), !string.IsNullOrEmpty(command.Keyword));
}
