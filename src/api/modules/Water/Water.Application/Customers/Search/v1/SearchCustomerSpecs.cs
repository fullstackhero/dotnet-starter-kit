using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Water.Application.Customers.Get.v1;
using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Customers.Search.v1;

public class SearchCustomerSpecs : EntitiesByPaginationFilterSpec<Customer, CustomerResponse>
{
    public SearchCustomerSpecs(SearchCustomersCommand command)
        : base(command) =>
        Query
            .OrderBy(c => c.FullName, !command.HasOrderBy())
            .Where(c => c.FullName.Contains(command.Keyword), !string.IsNullOrEmpty(command.Keyword));
}
