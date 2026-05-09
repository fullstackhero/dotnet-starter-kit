using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Water.Application.Bills.Get.v1;
using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Bills.Search.v1;

public class SearchBillSpecs : EntitiesByPaginationFilterSpec<Bill, BillResponse>
{
    public SearchBillSpecs(SearchBillsCommand command)
        : base(command) =>
        Query
            .OrderByDescending(c => c.BillingYear, !command.HasOrderBy())
            .ThenByDescending(c => c.BillingMonth)
            .Where(b => b.CustomerId == command.CustomerId, command.CustomerId.HasValue)
            .Where(b => b.Status == command.Status, command.Status.HasValue)
            .Where(b => b.BillingMonth == command.BillingMonth, command.BillingMonth.HasValue)
            .Where(b => b.BillingYear == command.BillingYear, command.BillingYear.HasValue);
}
