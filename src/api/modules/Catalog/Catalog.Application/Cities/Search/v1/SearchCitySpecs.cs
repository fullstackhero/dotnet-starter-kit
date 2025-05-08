using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Search.v1;
public class SearchCitiespecs : EntitiesByPaginationFilterSpec<City, CityResponse>
{
    public SearchCitiespecs(SearchCitiesCommand command)
        : base(command) =>
        Query
            .Include(p => p.Region)
            .OrderBy(c => c.Name, !command.HasOrderBy())
            .Where(p => p.RegionId == command.RegionId!.Value, command.RegionId.HasValue);
            }
