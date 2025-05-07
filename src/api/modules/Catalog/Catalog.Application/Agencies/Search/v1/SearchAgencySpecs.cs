using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Search.v1;
public class SearchAgencySpecs : EntitiesByPaginationFilterSpec<Agency, AgencyResponse>
{
    public SearchAgencySpecs(SearchAgenciesCommand command)
        : base(command) =>
        Query
            .OrderBy(c => c.Name, !command.HasOrderBy())
            .Where(a => a.Name.Contains(command.Keyword), !string.IsNullOrEmpty(command.Keyword));
}