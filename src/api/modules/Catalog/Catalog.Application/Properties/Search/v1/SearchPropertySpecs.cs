using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Properties.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Search.v1;
public class SearchPropertySpecs : EntitiesByPaginationFilterSpec<Property, PropertyResponse>
{
    public SearchPropertySpecs(SearchPropertiesCommand command)
        : base(command) =>
        Query
            .OrderBy(c => c.Name, !command.HasOrderBy())
            .Where(a => a.Name.Contains(command.Keyword) || a.Description.Contains(command.Keyword), !string.IsNullOrEmpty(command.Keyword));
}
