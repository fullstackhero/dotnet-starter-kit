using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Regions.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Search.v1;

public class SearchRegionSpecs : EntitiesByPaginationFilterSpec<Region, RegionResponse>
{
    public SearchRegionSpecs(SearchRegionsCommand command)
        : base(command) =>
        Query
            .OrderBy(c => c.Name, !command.HasOrderBy())
            .Where(r => r.Name.Contains(command.Keyword), !string.IsNullOrEmpty(command.Keyword));
}
