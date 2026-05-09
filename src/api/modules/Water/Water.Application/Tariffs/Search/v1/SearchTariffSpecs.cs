using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Water.Application.Tariffs.Get.v1;
using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Search.v1;

public class SearchTariffSpecs : EntitiesByPaginationFilterSpec<Tariff, TariffResponse>
{
    public SearchTariffSpecs(SearchTariffsCommand command)
        : base(command) =>
        Query
            .OrderBy(c => c.Name, !command.HasOrderBy())
            .Where(b => b.Name.Contains(command.Keyword), !string.IsNullOrEmpty(command.Keyword))
            .Where(b => b.IsActive == command.IsActive, command.IsActive.HasValue);
}
